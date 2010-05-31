namespace HttpServer.Rendering
{
    /// <summary>
    /// Interface for dynamically generated templates.
    /// </summary>
    /// <seealso cref="TemplateManager"/>
    public interface TinyTemplate
    {
        /// <summary>
        /// Run the template to generate HTML code.
        /// </summary>
        /// <param name="args">arguments passed to the template</param>
        /// <param name="hiddenTemplateManager">template manager (a manager is used to generate templates)</param>
        /// <returns>HTML code.</returns>
        string Invoke(TemplateArguments args, TemplateManager hiddenTemplateManager);
    }
}
