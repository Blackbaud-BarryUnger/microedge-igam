using System;
using System.Linq;
using System.Reflection;

namespace MicroEdge
{
	public static class ReflectionTools
	{
		/// <summary>
		/// Instantiate the indicated class through reflection. Note that this will work even with a class that has a private constructor.
		/// </summary>
		/// <typeparam name="T">
		/// Class of which you need an instance.
		/// </typeparam>
		/// <param name="getArguments">
		/// Functions that will return the values of the parameters needed for the constructor.
		/// </param>
		/// <returns>
		/// An instance of the indicated class.
		/// </returns>
		public static T Constructor<T>(params Func<object>[] getArguments) where T : class
		{
			var type = typeof(T);
			var args = (from getter in getArguments select getter()).ToArray();
			var argTypes = (from arg in args select arg.GetType()).ToArray();
			var ctor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, argTypes, null);
			return ctor.Invoke(args) as T;
		}

		/// <summary>
		/// Instantiate the indicated class through reflection. Note that this will work even with a class that has a private constructor.
		/// </summary>
		/// <typeparam name="T">
		/// Class of which you need an instance.
		/// </typeparam>
		/// <param name="args">
		/// Parameters needed for the constructor.
		/// </param>
		/// <returns>
		/// An instance of the indicated class.
		/// </returns>
		public static T Constructor<T>(params object[] args) where T : class
		{
			var type = typeof(T);
			var argTypes = (from arg in args select arg.GetType()).ToArray();
			var ctor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, argTypes, null);
			try
			{
				return ctor.Invoke(args) as T;
			}
			catch (TargetInvocationException e)
			{
				// The constructor threw an exception, so let's throw that exception, 
				//	not an invocation exception.
				throw e.InnerException;
			}
		}

		/// <summary>
		/// Instantiate the indicated class through reflection. Note that this will work even with a class that has a private constructor.
		/// </summary>
		/// <param name="type">
		/// Type of class of which you need an instance.
		/// </param>
		/// <param name="args">
		/// Parameters needed for the constructor.
		/// </param>
		/// <returns>
		/// An instance of the indicated class.
		/// </returns>
		public static object Constructor(Type type, params object[] args)
		{
			var argTypes = (from arg in args select arg.GetType()).ToArray();
			var ctor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, argTypes, null);
			return ctor.Invoke(args);
		}

		/// <summary>
		/// Get the value of a property through reflection. This will work with a private property.
		/// </summary>
		/// <typeparam name="T">
		/// Data type of the property.
		/// </typeparam>
		/// <param name="instance">
		/// Object that has the private property.
		/// </param>
		/// <param name="propertyName">
		/// Name of the property whose value you want.
		/// </param>
		/// <param name="index"></param>
		/// <returns>
		/// The property's current value.
		/// </returns>
		public static T GetProperty<T>(object instance, string propertyName, object index)
		{
			return (T)GetProperty(typeof(T), instance, propertyName, index);
		}
		public static T GetProperty<T>(object instance, string propertyName)
		{
			return GetProperty<T>(instance, propertyName, null);
		}

		/// <summary>
		/// Get the value of a property through reflection. This will work with a private property.
		/// </summary>
		/// <param name="propertyType">
		/// Data type of the property.
		/// </param>
		/// <param name="instance">
		/// Object that has the private property.
		/// </param>
		/// <param name="propertyName">
		/// Name of the property whose value you want.
		/// </param>
		/// <param name="index"></param>
		/// <returns>
		/// The property's current value.
		/// </returns>
		private static object GetProperty(Type propertyType, object instance, string propertyName, object index)
		{
			// Get the property info off the instance's type.
			PropertyInfo info = GetPropertyInfo(instance, propertyName);

			// Make sure the types match or is a sub-class of.
			if (info.PropertyType != propertyType && !info.PropertyType.IsSubclassOf(propertyType))
			{
				//Try by name before completely writing this off
				if (info.PropertyType.FullName != propertyType.FullName)
				{
					throw new System.Exception("Invalid data type specified");
				}
			}

			if (index == null)
				return info.GetValue(instance, new object[] { });
			
			return info.GetValue(instance, new object[] { index });
		}
		private static object GetProperty(Type propertyType, object instance, string propertyName)
		{
			return GetProperty(propertyType, instance, propertyName, null);
		}

		/// <summary>
		/// Get the info for a property through reflection. This will work with a private property.
		/// </summary>
		/// <param name="instance">
		/// Object that has the private property.
		/// </param>
		/// <param name="propertyName">
		/// Name of the property whose value you want.
		/// </param>
		/// <remarks>
		/// Property info.
		/// </remarks>
		public static PropertyInfo GetPropertyInfo(object instance, string propertyName)
		{
			if (instance == null)
			{
				throw new ArgumentNullException("instance");
			}

			if (string.IsNullOrEmpty(propertyName))
			{
				throw new ArgumentNullException("propertyName");
			}

			// Get the property info off the instance's type.
			// Unless the instance IS a type
			Type type = instance as Type;
			if (type == null)
				type = instance.GetType();
			PropertyInfo info = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

			// Make sure we found an info.
			if (info == null)
			{
				throw new MissingMemberException();
			}

			return info;
		}

		/// <summary>
		/// Get the value of a property through reflection. This will work with a private property.
		/// </summary>
		/// <param name="instance">
		/// Object that has the private property.
		/// </param>
		/// <param name="propertyName">
		/// Name of the property whose value you want.
		/// </param>
		/// <param name="propertyType"></param>
		/// <returns>
		/// The property's current value.
		/// </returns>
		public static object GetProperty(object instance, string propertyName, out Type propertyType)
		{
			PropertyInfo info = GetPropertyInfo(instance, propertyName);

			// Type of the property.
			propertyType = info.PropertyType;

			return info.GetValue(instance, new object[] { });
		}

		/// <summary>
		/// Test if the property exists on this object with the indicated type.
		/// </summary>
		/// <typeparam name="T">
		/// Data type of the property.
		/// </typeparam>
		/// <param name="instance">
		/// Object to test for existence of the property.
		/// </param>
		/// <param name="propertyName">
		/// Name of the property to test for.
		/// </param>
		/// <returns>
		/// True if the property exists and is of the indicated type. False otherwise.
		/// </returns>
		public static bool PropertyExists<T>(object instance, string propertyName)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			if (string.IsNullOrEmpty(propertyName))
				throw new ArgumentNullException("propertyName");

			var info = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			//Test that property exists and is of the right type.
			if (info != null && info.PropertyType == typeof(T))
				return true;
			
			return false;
		}

		/// <summary>
		/// Get the value of a field through reflection. This will work with a private field.
		/// </summary>
		/// <typeparam name="T">
		/// Data type of the field.
		/// </typeparam>
		/// <param name="instance">
		/// Object with the private field we need.
		/// </param>
		/// <param name="fieldName">
		/// Name of the field.
		/// </param>
		/// <returns>
		/// The value of the field.
		/// </returns>
		public static T GetField<T>(object instance, string fieldName)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			if (string.IsNullOrEmpty(fieldName))
				throw new ArgumentNullException("fieldName");

			var info = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			if (info == null)
				throw new MissingMemberException();

			//commparing complex types can be a little hinky; make sure NOTHING matches
			//before throwing an exception
			Type genericType = typeof(T);
			if (info.FieldType != genericType && info.FieldType.Name != genericType.Name && info.FieldType.FullName != genericType.FullName)
				throw new System.Exception("Invalid data type specified");

			return (T)info.GetValue(instance);
		}

		/// <summary>
		/// Test if the field exists in this object with the indicated type.
		/// </summary>
		/// <typeparam name="T">
		/// Data type of the field.
		/// </typeparam>
		/// <param name="instance">
		/// Object to test for existence of the field.
		/// </param>
		/// <param name="fieldName">
		/// Name of the field to test for.
		/// </param>
		/// <returns>
		/// True if the field exists and is of the indicated type. False otherwise.
		/// </returns>
		public static bool FieldExists<T>(object instance, string fieldName)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			if (string.IsNullOrEmpty(fieldName))
				throw new ArgumentNullException("fieldName");

			var info = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			if (info != null && info.FieldType == typeof(T))
				return true;
			
			return false;
		}

		/// <summary>
		/// Set the value of a field through reflection. This will work with a private field.
		/// </summary>
		/// <param name="instance">
		/// Object with the private field we need.
		/// </param>
		/// <param name="fieldName">
		/// Name of the field.
		/// </param>
		/// <param name="newVal">
		/// The new value for the field.
		/// </param>
		/// <param name="type">
		/// Type to be used if instance is null (so you can do actions on static classes).
		/// </param>
		private static void SetField(object instance, string fieldName, object newVal, Type type)
		{
			if (instance != null)
				type = instance.GetType();

			if (string.IsNullOrEmpty(fieldName))
				throw new ArgumentNullException("fieldName");

			var info = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

			while (info == null && type.BaseType != null)
			{
				type = type.BaseType;
				info = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
			}

			if (info == null)
				throw new MissingMemberException();

			if (newVal != null)
			{
				Type newValType = newVal.GetType();

				if (info.FieldType.IsInterface)
				{
					if (!newValType.GetInterfaces().Contains(info.FieldType))
						throw new System.Exception("Value does not implement interface " + info.FieldType);
				}
				else if (info.FieldType != newValType)
				{
					if (info.FieldType.ToString() != string.Format("System.Nullable`1[{0}]", newVal.GetType()))
						throw new System.Exception("Invalid data type specified");
				}
			}

			info.SetValue(instance, newVal);
		}
		public static void SetField(object instance, string fieldName, object newVal)
		{
			SetField(instance, fieldName, newVal, null);
		}
		public static void SetField(Type type, string fieldName, object newVal)
		{
			SetField(null, fieldName, newVal, type);
		}

		public static void SetIndexedProperty(object instance, object newVal, object[] indexParameters)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			var type = instance.GetType();
			var info = type.GetProperty("Item", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			info.SetValue(instance, newVal, indexParameters);
		}

		/// <summary>
		/// Set the value of a property through reflection. This will work with a private property.
		/// </summary>
		/// <param name="instance">
		/// Object with the property with the private setter.
		/// </param>
		/// <param name="propertyName">
		/// Name of the property to be set.
		/// </param>
		/// <param name="newVal">
		/// Value to set it to.
		/// </param>
		public static void SetProperty(object instance, string propertyName, object newVal)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
		
			//If a type was sent in, assume we're setting a static property
			Type type = instance as Type;
			if (type == null)
				SetProperty(instance, instance.GetType(), propertyName, newVal);
			else
				SetProperty(null, type, propertyName, newVal);
		}

		/// <summary>
		/// Set the value of a property through reflection. This will work with a private property.
		/// </summary>
		/// <param name="instance">
		/// Object with the property with the private setter.
		/// </param>
		/// <param name="type">
		/// Type of the object (by specifying this, it allows us to set static properties/properties on static classes)
		/// </param>
		/// <param name="propertyName">
		/// Name of the property to be set.
		/// </param>
		/// <param name="newVal">
		/// Value to set it to.
		/// </param>
		public static void SetProperty(object instance, Type type, string propertyName, object newVal)
		{
			if (string.IsNullOrEmpty(propertyName))
				throw new ArgumentNullException("propertyName");

			var info = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

			while (info == null && type.BaseType != null)
			{
				type = type.BaseType;
				info = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
			}

			if (info == null)
				throw new MissingMemberException();

			if (newVal != null)
			{
				if (!info.PropertyType.IsInterface && 
					info.PropertyType != typeof(object) && 
					info.PropertyType != newVal.GetType() &&
					info.PropertyType.ToString() != string.Format("System.Nullable`1[{0}]", newVal.GetType())
					)
				{
					throw new System.Exception("Invalid type for specified data");
				}
			}

			info.SetValue(instance, newVal, new object[] { });

		}

		/// <summary>
		/// Obtains info for a class's method
		/// </summary>
		/// <param name="instType">
		/// Type of class that owns the method
		/// </param>
		/// <param name="methodName">
		/// Name of the method
		/// </param>
		/// <param name="type">
		/// If the method accepts a generic type, this is that type. Currently, only a single generic type is supported.
		/// </param>
		/// <param name="args">
		/// Object array of arguments that will be required by the method.
		/// </param>
		/// <param name="staticMethod">
		/// If true, we'll look at static methods; false and it's instance ones
		/// </param>
		/// <returns>
		/// Matching method (null if one can't be found)
		/// </returns>
		public static MethodInfo GetMethod(Type instType, string methodName, Type type, object[] args, bool staticMethod)
		{
			MethodInfo method = null;
			BindingFlags flags = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic 
				| BindingFlags.Instance | (staticMethod ? BindingFlags.Static : BindingFlags.Instance);


			if (args == null)
				method = instType.GetMethod(methodName, flags, null, new Type[] { }, null);
			else
			{
				int argCount = args.Length;
				Type[] types = new Type[argCount];
				for (int i = 0; i < argCount; i++)
				{
					types[i] = args[i].GetType();
				}

				if (type == null)
				{
					method = instType.GetMethod(methodName, flags, null, types, null);
				}
				else
				{
					//try to for the generics
					MethodInfo[] allMethods = instType.GetMethods(flags);
					foreach (MethodInfo tempMethod in allMethods.Where(m => m.IsGenericMethod && m.Name == methodName && m.GetParameters().Count() == types.Count()))
					{
						method = tempMethod.MakeGenericMethod(type);

						if (method != null)
							break;
					}
				}
			}

			return method;
		}

		/// <summary>
		/// Invoke a method through reflection which returns no value
		/// </summary>
		/// <param name="instType">
		/// Type of class that owns the method
		/// </param>
		/// <param name="methodName">
		/// Name of the private method to invoke.
		/// </param>
		/// <param name="type">
		/// If the method accepts a generic type, this is that type. Currently, only a single generic type is supported.
		/// </param>
		/// <param name="args">
		/// Object array of arguments that will be required by the method.
		/// </param>
		public static void InvokeStaticMethod(Type instType, string methodName, Type type, params object[] args)
		{
			if (instType == null)
				throw new ArgumentNullException("instance");

			if (string.IsNullOrEmpty(methodName))
				throw new ArgumentNullException("methodName");

			MethodInfo method = GetMethod(instType, methodName, type, args, true);

			try
			{
				if (method.ReturnType != typeof(void))
					throw new System.Exception("Non-void return type found");

				//Return the invoked return value
				method.Invoke(null, args);
			}
			catch (TargetInvocationException ex)
			{
				//If it's an TargetInvocationException, throw the exception that caused it.
				throw ex.InnerException;
			}
		}

				/// <summary>
		/// Invoke a method through reflection which returns a value.
		/// </summary>
		/// <typeparam name="T">
		/// Data type of the value returned by the method.
		/// </typeparam>
		/// <param name="instType">
		/// Type of class that owns the method
		/// </param>
		/// <param name="methodName">
		/// Name of the private method to invoke.
		/// </param>
		/// <param name="type">
		/// If the method accepts a generic type, this is that type. Currently, only a single generic type is supported.
		/// </param>
		/// <param name="args">
		/// Object array of arguments that will be required by the method.
		/// </param>
		/// <returns>
		/// Value returned by the method.
		/// </returns>
		public static T InvokeStaticMethod<T>(Type instType , string methodName, Type type, params object[] args)
		{
			if (instType == null)
				throw new ArgumentNullException("instance");

			if (string.IsNullOrEmpty(methodName))
				throw new ArgumentNullException("methodName");

			MethodInfo method = GetMethod(instType, methodName, type, args, true);

			try
			{
				if (method == null)
					return default(T);

				if (method.ReturnType == typeof(void) || method.ReturnType != typeof(T))
					throw new System.Exception("Invalid return type specified");

				//Return the invoked return value
				return (T)method.Invoke(null, args);
			}
			catch (TargetInvocationException ex)
			{
				//If it's an TargetInvocationException, throw the exception that caused it.
				throw ex.InnerException;
			}
		}

		/// <summary>
		/// Invoke an instance method through reflection which returns a value.
		/// </summary>
		/// <typeparam name="T">
		/// Data type of the value returned by the method.
		/// </typeparam>
		/// <param name="instance">
		/// Instance of the class with the private method.
		/// </param>
		/// <param name="methodName">
		/// Name of the private method to invoke.
		/// </param>
		/// <param name="type">
		/// If the method accepts a generic type, this is that type. Currently, only a single generic type is supported.
		/// </param>
		/// <param name="args">
		/// Object array of arguments that will be required by the method.
		/// </param>
		/// <returns>
		/// Value returned by the method.
		/// </returns>
		public static T InvokeMethod<T>(object instance, string methodName, Type type, params object[] args)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			if (string.IsNullOrEmpty(methodName))
				throw new ArgumentNullException("methodName");

			Type instType = instance.GetType();
			MethodInfo method = GetMethod(instType, methodName, type, args, false);

			try
			{
				if (method == null)
				{
					try
					{
						//Might be Out or Ref argruements....so let's try to just invoke it out right.
						return (T)instType.InvokeMember(methodName, BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, instance, args, null, null, null);
					}
					catch (TargetInvocationException ex)
					{
						//If it's an TargetInvocationException, throw the exception that caused it.
						throw ex.InnerException;
					}
					catch
					{
						//If that didn't work, assume the method is missing.
						throw new MissingMethodException();
					}
				}

				if (method.ReturnType == typeof(void) || method.ReturnType != typeof(T))
					throw new System.Exception("Invalid return type specified");

				//Return the invoked return value
				return (T)method.Invoke(instance, args);
			}
			catch (TargetInvocationException ex)
			{
				//If it's an TargetInvocationException, throw the exception that caused it.
				throw ex.InnerException;
			}
		}
		public static T InvokeMethod<T>(object instance, string methodName, params object[] args)
		{
			return InvokeMethod<T>(instance, methodName, null, args);
		}
		public static T InvokeMethod<T>(object instance, string methodName)
		{ 
			return InvokeMethod<T>(instance, methodName, null, null); 
		}

		/// <summary>
		/// Invoke a method through reflection which returns no value.
		/// </summary>
		/// <param name="type">
		/// Data type of the value returned by the method.
		/// </param>
		/// <param name="instance">
		/// Instance of the class with the private method.
		/// </param>
		/// <param name="methodName">
		/// Name of the private method to invoke.
		/// </param>
		/// <param name="args">
		/// Object array of arguments that will be required by the method.
		/// </param>
		public static void InvokeMethod(object instance, string methodName, Type type, params object[] args)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			if (string.IsNullOrEmpty(methodName))
				throw new ArgumentNullException("methodName");

			Type instType = instance.GetType();
			//If the instance sent in was a type, just use that
			if (instType == typeof (Type))
				instType = instance as Type;

			MethodInfo method = null;

			if (args == null)
				method = instType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);
			else
			{
				int argCount = args.Length;
				Type[] types = new Type[argCount];
				for (int i = 0; i < argCount; i++)
				{
					types[i] = args[i].GetType();
				}

				// Loop through current type then parents until we've found something
				Type currentType = instType;
				while (method == null && currentType != null)
				{

					method = currentType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);

					//If we couldn't get the method  the normal way, try to for the generics
					if (method == null && type != null)
					{
						MethodInfo[] allMethods = currentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
						foreach (MethodInfo tempMethod in allMethods.Where(m => m.IsGenericMethod && m.Name == methodName && m.GetParameters().Count() == types.Count()))
						{
							method = tempMethod.MakeGenericMethod(type);
							if (method != null) 
								break;
						}
					}

					// Go to the parent type
					currentType = currentType.BaseType;
				}
			}

			try
			{
				if (method == null)
				{
					try
					{
						//Might be Out or Ref argruements....so let's try to just invoke it out right.
						instType.InvokeMember(methodName, BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, instance, args, null, null, null);

						//It worked, return
						return;
					}
					catch
					{
						//If that didn't work, assume the method is missing.
						throw new MissingMethodException();
					}
				}


				method.Invoke(instance, args);
			}
			catch (TargetInvocationException ex)
			{
				//If it's an TargetInvocationException, throw the exception that caused it.
				throw ex.InnerException;
			}
		}

		public static void InvokeMethod(object instance, string methodName)
		{ 
			InvokeMethod(instance, methodName, null, null);
		}
		public static void InvokeMethod(object instance, string methodName, params object[] args)
		{ 
			InvokeMethod(instance, methodName, null, args); 
		}
	}
}
