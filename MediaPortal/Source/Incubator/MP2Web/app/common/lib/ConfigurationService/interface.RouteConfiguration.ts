/*
 This interface must mirror the MP2WebAppRouterConfiguration Class from the ConfigurationController!
 */
export class MP2WebAppRouterConfiguration
{
    public Name: string;

    public Label: string;

    public Category: string;

    public Path: string;

    public ComponentPath: string;

    public Component: string;

    public Visible: boolean;

    public Pages: MP2WebAppRouterConfiguration[];
}