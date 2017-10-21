export class IAttachedClients
{
    // UUID of the attached client.
    public SystemId: string;

    // Last known host name of the client.
    public LastSystem: string;

    // Last known client name.
    public LastClientName: string;

    // Indicates if the Client is online
    public Online: boolean;
}