import {MP2WebAppRouterConfiguration} from "./interface.RouteConfiguration";

/*
This interface must mirror the Configuration Class from the ConfigurationController!
 */
export interface IConfiguration {
  WebApiUrl: string;
  Routes: MP2WebAppRouterConfiguration[];

  MoviesPerRow: number;
  MoviesPerQuery: number;

  DefaultEpgGroupId: number;
}