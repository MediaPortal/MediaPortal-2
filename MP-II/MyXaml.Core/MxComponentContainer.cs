using System;

namespace MyXaml.Core
{
    /// <summary>
    /// A helper class to manage a component container for IContainer objects. The .NET
    /// Container implementation does not support a writeable collection, therefore we have
    /// to provide this helper class.
    /// </summary>
    public class MxComponentContainer : MyXaml.IMyXaml, System.ComponentModel.ISupportInitialize
    {
        #region Variables

        // For MyXaml.IMyXaml interface
        private string m_Name;
        private object m_Tag;

        protected System.ComponentModel.Container container;
        protected System.Collections.ArrayList components;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public MxComponentContainer()
        {
            container = new System.ComponentModel.Container();
            components = new System.Collections.ArrayList();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The underlying Container instance.
        /// </summary>
        public System.ComponentModel.Container Container
        {
            get { return container; }
        }

        /// <summary>
        /// A collection of IComponent instances. This array is cleared after EndInit().
        /// </summary>
        public System.Collections.ArrayList Components
        {
            get { return components; }
        }

        #endregion

        #region System.ComponentModel.ISupportInitialize Members

        public virtual void BeginInit()
        {
        }

        /// <summary>
        /// Populates the Container instance with the IComponent instances in the Components array.
        /// </summary>
        public virtual void EndInit()
        {
            foreach (System.ComponentModel.IComponent obj in components)
            {
                container.Add(obj);
            }
            // clear the array so it can be ref'd later on and additional items added.
            components.Clear();
        }

        #endregion

        #region MyXaml.IMyXaml Members

        #region Properties

        /// <summary>
        /// The name of this instance. Necessary for MyXaml
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        /// <summary>
        /// The tag of this instance. Necessary for MyXaml
        /// </summary>
        public object Tag
        {
            get { return m_Tag; }
            set { m_Tag = value; }
        }

        #endregion

        #endregion
    }
}
