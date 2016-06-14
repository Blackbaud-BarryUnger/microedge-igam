using System.Collections.Generic;
using System.ComponentModel;

namespace MicroEdge
{
	/// <summary>
	/// Class for a background worker that works sequentially on/with a queue of items.
	/// </summary>
	/// <typeparam name="TItem">The type of items.</typeparam>
	public class QueuedWorker<TItem>
	{
		#region Fields

		/// <summary>
		/// Lock to make sure we aren't editing any of the class level variables 
		/// in different threads at the same time.
		/// </summary>
		private readonly object _startFinishLock = new object();

		private readonly BackgroundWorker _worker;
		private readonly LinkedList<TItem> _queue;
		private SetupItemMethod<TItem> _setupNextItemFunc;
		private CleanupItemMethod<TItem> _cleanupFinishedItemFunc;
		private TItem _currentlyWorkingOnItem;

		private bool _isRestartNeeded;
		private bool _isPaused;

		#endregion

		#region Delegates

		/// <summary>
		/// Function which prepares the next item for work.
		/// </summary>
		/// <typeparam name="TItems">Type of the item.</typeparam>
		/// <param name="worker">The worker that will be doing the work.</param>
		/// <param name="item">The item which is being set up to be worked with.</param>
		/// <returns>Any argument that needs to be passed to the worker's RunWorkerAsync</returns>
		public delegate object SetupItemMethod<TItems>(BackgroundWorker worker, TItems item);

		/// <summary>
		/// Function which cleans up an item after work.
		/// </summary>
		/// <typeparam name="TItems">The type of item.</typeparam>
		/// <param name="worker">The worker that did the work.</param>
		/// <param name="item">The item the needs clean up.</param>
		public delegate void CleanupItemMethod<TItems>(BackgroundWorker worker, TItems item);

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		public QueuedWorker()
		{
			_worker = new BackgroundWorker();
			_worker.WorkerSupportsCancellation = true;
			_worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

			_queue = new LinkedList<TItem>();
		}

		#endregion

		#region Properties

		/// <summary>
		/// The function to set up the next item.  
		/// </summary>
		public SetupItemMethod<TItem> SetupNextItemFunction
		{
			get
			{
				return _setupNextItemFunc;
			}
			set
			{
				_setupNextItemFunc = value;
			}
		}

		/// <summary>
		/// The function to clean up an item when done with work with it.
		/// </summary>
		public CleanupItemMethod<TItem> CleanupFinishedItemFunction
		{
			get
			{
				return _cleanupFinishedItemFunc;
			}
			set
			{
				_cleanupFinishedItemFunc = value;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Handles the RunWorkerCompleted event from the 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			lock (_startFinishLock)
			{
				// Clean up the last item
				if (_currentlyWorkingOnItem != null)
				{
					_cleanupFinishedItemFunc(_worker, _currentlyWorkingOnItem);
				}

				// Handle cancels.
				if (e.Cancelled)
				{
					if (_isRestartNeeded)
					{
						// Don't return, we are restarting, so let this go through
						//	and begin work on the next item.
					}
					else if (_isPaused)
					{
						// Work get canceled in the middle of an item by a pause.
						// Put the item back on the queue so we can work on it again upon resume
						_queue.AddFirst(_currentlyWorkingOnItem);
						return;
					}
					else
					{
						// Just a normal cancel, return so we don't do any more work.
						return;
					}
				}
				else
				{
					// This isn't canceled, but we still may need to stop
					if (_isPaused)
					{
						// Paused work, but the item that we were working on finished
						//	successfully, so we don't need to re-add it to the queue.
						return;
					}
				}

				// Start work with the next item, if needed.
				BeginNextItemWork();
			}
		}


		/// <summary>
		/// Stops the worker and clears the queue, so that no more
		/// items in the queue are worked on.
		/// </summary>
		public void StopWorkAndClearQueue()
		{
			lock (_startFinishLock)
			{
				// Clear the queue first, so the worker doesn't think there are more to work on.
				_queue.Clear();

				// Stop the worker.
				_worker.CancelAsync();
			}
		}

		/// <summary>
		/// Stops the worker and prevents it from continuing to work on any more
		/// items in the queue.
		/// </summary>
		/// <remarks>
		/// If work gets canceled partway through an item when pausing, that item
		/// will be re-added to the front of the queue, so it will be first item
		/// worked on upon resuming.
		/// </remarks>
		public void StopWorkAndPause()
		{
			lock (_startFinishLock)
			{
				_isPaused = true;

				// Make sure to isRestartNeded is false, we don't get confused
				//	and restart when pausing
				_isRestartNeeded = false;

				// Cancel the work, and let the RunWorkerCompleted handler
				//	deal with the rest.
				_worker.CancelAsync();
			}
		}

		/// <summary>
		/// Resumes work on any that were unfinished due to pausing.
		/// </summary>
		public void ResumeWork()
		{
			lock (_startFinishLock)
			{
				_isPaused = false;

				// If the worker is busy, un-pausing the flag is all we need to do.
				if (_worker.IsBusy)
				{
					return;
				}

				// Start on the next item, if there is one.
				BeginNextItemWork();
			}
		}

		/// <summary>
		/// STOPS work on current items (if there are any), clears them out, then starts
		/// work on the new items.
		/// </summary>
		/// <param name="newItems">New items to work on.</param>
		public void StartWorkOnNewItems(IEnumerable<TItem> newItems)
		{
			lock (_startFinishLock)
			{
				// Clear any flags left over from before this
				_isPaused = false;
				_isRestartNeeded = false;

				// Clear the queue.
				_queue.Clear();

				// Queue up all the counts again
				foreach (TItem newItem in newItems)
				{
					_queue.AddLast(newItem);
				}

				// Before starting, make sure we stop any previous work.
				if (_worker.IsBusy)
				{
					_isRestartNeeded = true;
					_worker.CancelAsync();
				}
				else
				{
					BeginNextItemWork();
				}
			}
		}

		/// <summary>
		/// Begins work on the next item in the queue.
		/// </summary>
		private void BeginNextItemWork()
		{
			// This is called by other methods that lock the object, so don't do it here.

			// We will never need a restart if this gets hit, so make sure to clear this out
			//	since we are doing the restart now (or just continuing).
			_isRestartNeeded = false;

			// Begin work on the next item, if there is one.
			if (_queue.Count > 0)
			{
				// De-queue the item.
				TItem newCurrentItem = _queue.First.Value;
				_queue.RemoveFirst();

				// Hold on to it for cleaning up later.
				_currentlyWorkingOnItem = newCurrentItem;
				// Set it up and get the argument needed.
				object argument = _setupNextItemFunc(_worker, newCurrentItem);

				// Start the worker
				_worker.RunWorkerAsync(argument);
			}
		}

		#endregion

	}
}
