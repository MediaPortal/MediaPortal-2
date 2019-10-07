using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Nereus.Controls
{
  /// <summary>
  /// Control that allows a template to safely access items in an <see cref="ItemsList"/> by index,
  /// returning null rather than an <see cref="IndexOutOfRangeException"/> if the index is invalid.
  /// </summary>
  public class NereusTileControl : Control, IObservable
  {
    protected AbstractProperty _itemsSourceProperty;
    protected WeakEventMulticastDelegate _objectChanged = new WeakEventMulticastDelegate();

    public NereusTileControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _itemsSourceProperty = new SProperty(typeof(IEnumerable), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();

      base.DeepCopy(source, copyManager);

      NereusTileControl c = (NereusTileControl)source;
      ItemsSource = copyManager.GetCopy(c.ItemsSource);

      Attach();
    }

    void Attach()
    {
      _itemsSourceProperty.Attach(OnItemsSourceChanged);
      AttachToItemsSource(ItemsSource);
    }

    void Detach()
    {
      _itemsSourceProperty.Detach(OnItemsSourceChanged);
      DetachFromItemsSource(ItemsSource);
    }

    void AttachToItemsSource(object value)
    {
      IObservable ob = value as IObservable;
      if (ob != null)
        ob.ObjectChanged += OnItemsCollectionChanged;
    }

    void DetachFromItemsSource(object value)
    {
      IObservable ob = value as IObservable;
      if (ob != null)
        ob.ObjectChanged -= OnItemsCollectionChanged;
    }

    private void OnItemsSourceChanged(AbstractProperty property, object oldValue)
    {
      DetachFromItemsSource(oldValue);
      AttachToItemsSource(ItemsSource);
      FireChange();
    }

    private void OnItemsCollectionChanged(IObservable observable)
    {
      // The skin doesn't bind directly to the list, so any bindings won't pick up collection changed events.
      // We manually pass them on here instead.
      FireChange();
    }

    /// <summary>
    /// Event to track changes to this item.
    /// </summary>
    public event ObjectChangedDlgt ObjectChanged
    {
      add { _objectChanged.Attach(value); }
      remove { _objectChanged.Detach(value); }
    }

    public AbstractProperty ItemsSourceProperty
    {
      get { return _itemsSourceProperty; }
    }

    public IEnumerable ItemsSource
    {
      get { return (IEnumerable)_itemsSourceProperty.GetValue(); }
      set { _itemsSourceProperty.SetValue(value); }
    }

    public object this[int index]
    {
      get
      {        
        var items = ItemsSource as IList<ListItem>;
        return items != null && items.Count > index ? items[index] : null;
      }
    }

    /// <summary>
    /// Fires the <see cref="ObjectChanged"/> event.
    /// </summary>
    public void FireChange()
    {
      _objectChanged.Fire(new object[] { this });
    }

    public override void Dispose()
    {
      Detach();
      DetachFromItemsSource(ItemsSource);
      base.Dispose();
    }
  }
}
