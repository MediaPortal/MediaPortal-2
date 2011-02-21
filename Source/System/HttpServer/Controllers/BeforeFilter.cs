using System;

namespace HttpServer.Controllers
{
    /// <summary>
    /// Methods marked with BeforeFilter will be invoked before each request.
    /// </summary>
    /// <remarks>
    /// BeforeFilters should take no arguments and return false
    /// if controller method should not be invoked.
    /// </remarks>
    /// <seealso cref="FilterPosition"/>
    public class BeforeFilterAttribute : Attribute
    {
        private readonly FilterPosition _position;

        /// <summary>
        /// Initializes a new instance of the <see cref="BeforeFilterAttribute"/> class.
        /// </summary>
        /// <remarks>
        /// BeforeFilters should take no arguments and return false
        /// if controller method should not be invoked.
        /// </remarks>
        public BeforeFilterAttribute()
        {
            _position = FilterPosition.Between;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BeforeFilterAttribute"/> class.
        /// </summary>
        /// <param name="position">Specify if the filter should be invoked among the first filters, in between or among the last.</param>
        /// <remarks>
        /// BeforeFilters should take no arguments and return false
        /// if controller method should not be invoked.
        /// </remarks>
        public BeforeFilterAttribute(FilterPosition position)
        {
            _position = position;
        }

        /// <summary>
        /// Filters position in before filter queue
        /// </summary>
        public FilterPosition Position
        {
            get { return _position; }
        }
    }

    /// <summary>
    /// Determins when a before filter is executed.
    /// </summary>
    /// <seealso cref="BeforeFilterAttribute"/>
    public enum FilterPosition
    {
        /// <summary>
        /// Filter will be invoked first (unless another filter is added after this one with the First position)
        /// </summary>
        First,
        /// <summary>
        /// Invoke after all first filters, and before the last filters.
        /// </summary>
        Between,
        /// <summary>
        /// Filter will be invoked last (unless another filter is added after this one with the Last position)
        /// </summary>
        Last
    }
}