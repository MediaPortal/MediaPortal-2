using MediaPortal.UI.SkinEngine.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UI.SkinEngine.MpfElements
{
  /// <summary>
  /// Provides data for data transfer events.
  /// </summary>
  /// <remarks>
  /// If a binding has the NotifyOnTargetUpdated or NotifyOnSourceUpdated property
  /// set to true, data transfer events will be fired on the target object
  /// when the binding updates the target or source respectively.
  /// </remarks>
  public class DataTransferEventArgs : RoutedEventArgs
  {
    public DataTransferEventArgs(RoutedEvent id, DependencyObject targetObject, IDataDescriptor targetDataDescriptor) :
       base(id)
    {
      if (targetObject == null)
        throw new ArgumentNullException("targetObject");
      if (targetDataDescriptor == null)
        throw new ArgumentNullException("targetDataDescriptor");

      TargetObject = targetObject;
      TargetDataDescriptor = targetDataDescriptor;
    }

    /// <summary>
    /// The target object of the binding that raised the event.
    /// </summary>
    public DependencyObject TargetObject { get; private set; }

    /// <summary>
    /// The target data descriptor of the binding that raised the event.
    /// </summary>
    public IDataDescriptor TargetDataDescriptor { get; private set; }

    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
    {
      var handler = genericHandler as DataTransferEventHandler;
      if (handler != null)
      {
        handler(genericTarget, this);
      }
      else
      {
        base.InvokeEventHandler(genericHandler, genericTarget);
      }
    }
  }

  /// <summary>
  /// Represents the method that will handle data transfer events.
  /// </summary>
  /// <param name="sender">Sender of the event</param>
  /// <param name="e">Event arguments for this event.</param>
  public delegate void DataTransferEventHandler(object sender, DataTransferEventArgs e);
}
