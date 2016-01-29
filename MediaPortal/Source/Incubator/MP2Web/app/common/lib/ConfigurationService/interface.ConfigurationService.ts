/*
This interface must mirror the Configuration Class from the ConfigurationController!
 */
export interface IConfiguration {
    WebApiUrl: string;

    MoviesPerRow: number;
    MoviesPerQuery: number;
}