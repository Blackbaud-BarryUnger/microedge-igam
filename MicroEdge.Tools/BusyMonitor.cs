using System;
using System.Collections.Generic;

namespace MicroEdge
{
	/// <summary>
	/// This is a static class for keeping track of busy object which are processing
	/// and need to be waited for. This was designed to be used globally so that anything
	/// can indicate that it's busy and/or know if anything else is busy
	/// </summary>
	public static class BusyMonitor
	{
		#region Fields

		private static bool _isOn;

		private static HashSet<object> _busyItems;

		#endregion

		#region Constructors

		/// <summary>
		/// Static constructor.
		/// </summary>
		static BusyMonitor()
		{
			// Start on.
			IsOn = true;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Collection of busy items.
		/// </summary>
		private static HashSet<object> BusyItems
		{
			get
			{
				if (_busyItems == null)
				{
					_busyItems = new HashSet<object>();
				}

				return _busyItems;
			}
		}



		/// <summary>
		/// Indicates if there are any busy items.
		/// </summary>
		public static bool AreBusyItems
		{
			get
			{
				if (!IsOn)
				{
					return false;
				}

				return BusyItems.Count > 0;
			}
		}

		/// <summary>
		/// Indicates if this is on, meaning its in normal operation.
		/// When off, AreBusyItems always returns false, so that 
		/// the busy image never shows.
		/// </summary>
		public static bool IsOn
		{
			get
			{
				return _isOn;
			}
			set
			{
				if (value != IsOn)
				{
					_isOn = value;
					OnBusyItemsChanged(EventArgs.Empty);
				}
			}
		}

		#endregion

		#region Methods

		///// <summary>
		///// Handles collection changed events from the BusyItems collection.
		///// </summary>
		///// <param name="sender"></param>
		///// <param name="e"></param>
		//private static void BusyItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		//{
		//    OnBusyItemsChanged(EventArgs.Empty);
		//}


		/// <summary>
		/// Adds a busy item to the monitor.
		/// </summary>
		/// <param name="item">Busy item.</param>
		public static void AddBusyItem(object item)
		{
			// Make sure to only add items once
			if (!BusyItems.Contains(item))
			{
				BusyItems.Add(item);
				OnBusyItemsChanged(EventArgs.Empty);
			}
		}

		/// <summary>
		/// Removes a busy item to the monitor.
		/// </summary>
		/// <param name="item">Busy item.</param>
		public static void RemoveBusyItem(object item)
		{
			if (BusyItems.Remove(item))
			{
				OnBusyItemsChanged(EventArgs.Empty);
			}
		}


		/// <summary>
		/// Determines if an item is busy.
		/// </summary>
		/// <param name="item">The item to check.</param>
		public static bool IsItemBusy(object item)
		{
			return BusyItems.Contains(item);
		}

		#endregion

		#region Events

		/// <summary>
		/// Event raised then the busy items have changed.
		/// NOTE: Hooking into this event can prevent listening 
		///		object to never be cleaned up by Garbage Collector,
		///		this is designed to be used by ApplicationViewModel only, 
		///		which never needs to be cleaned up.
		/// </summary>
		public static event EventHandler BusyItemsChanged;

		/// <summary>
		/// Raises BusyItemsChanged event.
		/// </summary>
		/// <param name="eventArgs">Event args.</param>
		private static void OnBusyItemsChanged(EventArgs eventArgs)
		{
			if (BusyItemsChanged != null)
			{
				BusyItemsChanged(null, eventArgs);
			}
		}


		#endregion
	}
}
