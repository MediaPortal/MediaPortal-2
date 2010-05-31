using System;

namespace HttpServer.Rendering
{
    /// <summary>
    /// Keeps information about templates, so we know when to regenerate it.
    /// </summary>
    public interface ITemplateInfo
    {
        /// <summary>
        /// When the template was compiled.
        /// </summary>
        /// <remarks>Use this date to determine if the template is old and needs to be recompiled.</remarks>
        DateTime CompiledWhen
        { get; }

        /// <summary>
        /// Template file name.
        /// </summary>
        string Filename
        { get; }

        /// <summary>
        /// The actual template.
        /// </summary>
        ITinyTemplate Template
        { get; }
    }
}
