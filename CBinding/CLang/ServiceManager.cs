namespace CBinding;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;


[ExportCompletionProvider (nameof (CCastCompletionProvider), "C/C++")]
internal class CCastCompletionProvider : CompletionProvider
{
    public override Task ProvideCompletionsAsync (CompletionContext context)
    {
        throw new NotImplementedException ();
    }

    public override bool ShouldTriggerCompletion (SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
    {
        return base.ShouldTriggerCompletion (text, caretPosition, trigger, options);
    }
}

public sealed class ServiceManager : IDisposable
{
	private readonly IPropertyOwner _propertyOwner;
	private readonly object _lock = new object ();
	private bool _isDisposed = false;

	/// <summary>
	/// Fire when service is added
	/// </summary>
	internal event EventHandler<ServiceManagerEventArgs>? ServiceAdded;

	/// <summary>
	/// Fires when service is removed
	/// </summary>
	internal event EventHandler<ServiceManagerEventArgs>? ServiceRemoved;

	private readonly Dictionary<Type, object> _servicesByType = new Dictionary<Type, object> ();

	public static void AdviseServiceAdded<T> (IPropertyOwner propertyOwner, Action<T> callback) where T : class
	{
		ServiceManager sm = FromPropertyOwner (propertyOwner);

		T? existingService = sm.GetService<T> ();
		if (existingService != null) {
			callback (existingService);
		} else {
			void onServiceAdded (object sender, ServiceManagerEventArgs eventArgs)
			{
				if (eventArgs.ServiceType == typeof (T) && eventArgs.Service is T service) {
					callback (service);
					sm.ServiceAdded -= onServiceAdded;
				}
			}

			sm.ServiceAdded += onServiceAdded;
		}
	}

	internal static void AdviseServiceRemoved<T> (IPropertyOwner propertyOwner, Action<T> callback) where T : class
	{
		ServiceManager sm = FromPropertyOwner (propertyOwner);

		void onServiceRemoved (object sender, ServiceManagerEventArgs eventArgs)
		{
			if (eventArgs.ServiceType == typeof (T) && eventArgs.Service is T service) {
				callback (service);
				sm.ServiceRemoved -= onServiceRemoved;
			}
		}

		sm.ServiceRemoved += onServiceRemoved;
	}

	private ServiceManager (IPropertyOwner propertyOwner)
	{
		_propertyOwner = propertyOwner;
		_propertyOwner.Properties.AddProperty (typeof (ServiceManager), this);

		if (propertyOwner is ITextView textView) {
			textView.Closed += OnViewClosed;
		}
		else if (propertyOwner is ITextBuffer textBuffer) {
			// Need to wait to idle as the TextViewConnectListener.OnTextBufferDisposing hasn't fired yet.
			//textBuffer.AddBufferDisposedAction (DisposeServiceManagerOnIdle);
		}
	}

	private static void DisposeServiceManagerOnIdle (IPropertyOwner propertyOwner)
	{
		ServiceManager? sm = FromPropertyOwner (propertyOwner, false);
		if (sm is not null) {
			sm.Dispose ();

			//IdleTimeAction.Create (() => {
			//	sm.Dispose ();
			//}, 150, new object ());
		}
	}

	private void OnViewClosed (object sender, EventArgs e)
	{
		ITextView textView = (ITextView)sender;
		textView.Closed -= OnViewClosed;

		// Need to wait to idle as taggers can also get disposed during TextView.Closed notifications
		DisposeServiceManagerOnIdle (textView);
	}

	/// <summary>
	/// Returns service manager attached to a given Property owner
	/// </summary>
	/// <param name="propertyOwner">Property owner</param>
	/// <returns>Service manager instance</returns>
	internal static ServiceManager FromPropertyOwner (IPropertyOwner propertyOwner)
	{
		return FromPropertyOwner (propertyOwner, true)!;
	}

	internal static ServiceManager? FromPropertyOwner (IPropertyOwner propertyOwner, bool ensureCreated)
	{
		ServiceManager? sm;

		if (ensureCreated) {
			sm = propertyOwner.Properties.GetOrCreateSingletonProperty (() => new ServiceManager (propertyOwner));
		} else {
			_ = propertyOwner.Properties.TryGetProperty (typeof (ServiceManager), out sm);
		}

		return sm;
	}

	/// <summary>
	/// Retrieves service from a service manager for this Property owner given service type
	/// </summary>
	/// <typeparam name="T">Service type</typeparam>
	/// <param name="propertyOwner">Property owner</param>
	/// <returns>Service instance</returns>
	public static T? GetService<T> (IPropertyOwner propertyOwner) where T : class
	{
		try {
			ServiceManager sm = FromPropertyOwner (propertyOwner);

			return sm.GetService<T> ();
		} catch (Exception) {
			return null;
		}
	}

	internal static T GetRequiredService<T> (IPropertyOwner propertyOwner) where T : class
	{
		ServiceManager sm = FromPropertyOwner (propertyOwner);

		if (sm.GetService<T> () is not T service) {
			string errorMessage = "Required service not found";
			Debug.Fail (errorMessage);
			throw new InvalidOperationException (errorMessage);
		}

		return service;
	}

	internal static ICollection<T> GetAllServices<T> (IPropertyOwner propertyOwner) where T : class
	{
		ServiceManager sm = FromPropertyOwner (propertyOwner);

		return sm is not null ? sm.GetAllServices<T> () : new List<T> ();
	}

	/// <summary>
	/// Add service to a service manager associated with a particular Property owner
	/// </summary>
	/// <typeparam name="T">Service type</typeparam>
	/// <param name="serviceInstance">Service instance</param>
	/// <param name="propertyOwner">Property owner</param>
	public static void AddService<T> (T serviceInstance, IPropertyOwner propertyOwner) where T : class
	{
		ServiceManager sm = FromPropertyOwner (propertyOwner);

		sm.AddService (serviceInstance);
	}

	public static void RemoveService<T> (IPropertyOwner propertyOwner) where T : class
	{
		ServiceManager sm = FromPropertyOwner (propertyOwner);

		sm.RemoveService<T> ();
	}

	private T? GetService<T> () where T : class
	{
		return GetService<T> (true);
	}

	// TODO: do we really need the checkDerivation codepath? Seems like
	//   it would cause more unexpected pain than it's worth...
	private T? GetService<T> (bool checkDerivation) where T : class
	{
		lock (_lock) {
			if (!_servicesByType.TryGetValue (typeof (T), out object? service) && checkDerivation) {
				// try walk through and cast. Perhaps someone is asking for IFoo
				// that is implemented on class Bar but Bar was added as Bar, not as IFoo
				foreach (KeyValuePair<Type, object> kvp in _servicesByType) {
					service = kvp.Value as T;
					if (service != null) {
						break;
					}
				}
			}

			return service as T;
		}
	}

	private ICollection<T> GetAllServices<T> () where T : class
	{
		List<T> list = new List<T> ();

		lock (_lock) {
			foreach (KeyValuePair<Type, object> kvp in _servicesByType) {
				if (kvp.Value is T service) {
					list.Add (service);
				}
			}
		}

		return list;
	}

	private void AddService<T> (T serviceInstance) where T : class
	{
		bool added = false;

		lock (_lock) {
			if (GetService<T> (false) == null) {
				_servicesByType.Add (typeof (T), serviceInstance);
				added = true;
			}
		}

		Debug.Assert (added);
		if (added) {
			FireServiceAdded (typeof (T), serviceInstance);
		}
	}

	private void RemoveService<T> () where T : class
	{
		bool foundServiceInstance = false;
		object serviceInstance;

		lock (_lock) {
			foundServiceInstance = _servicesByType.TryGetValue (typeof (T), out serviceInstance);

			if (foundServiceInstance) {
				_servicesByType.Remove (typeof (T));
			}
		}

		if (foundServiceInstance) {
			FireServiceRemoved (typeof (T), serviceInstance);
		} else {
			Debug.Assert (false, "Unable to find service " + typeof (T).Name + " to remove from the ServiceManager!");
		}
	}

	private void FireServiceAdded (Type serviceType, object serviceInstance)
	{
		ServiceAdded?.Invoke (this, new ServiceManagerEventArgs (serviceType, serviceInstance));
	}

	private void FireServiceRemoved (Type serviceType, object serviceInstance)
	{
		ServiceRemoved?.Invoke (this, new ServiceManagerEventArgs (serviceType, serviceInstance));
	}

	public void Dispose ()
	{
		Debug.Assert (!_isDisposed);
		if (!_isDisposed) {
			_isDisposed = true;

			_propertyOwner.Properties.RemoveProperty (typeof (ServiceManager));

			Debug.Assert (_servicesByType.Count == 0);
			_servicesByType.Clear ();
		}
	}
}

internal sealed class ServiceManagerEventArgs : EventArgs
{
	public object Service { get; }
	public Type ServiceType { get; }

	public ServiceManagerEventArgs (Type type, object service)
	{
		Service = service;
		ServiceType = type;
	}
}
