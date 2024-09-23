﻿using TeamServer.Events;

namespace TeamServer.Hubs;

public interface INotificationHub
{
    #region Handlers

    Task HttpHandlerCreated(string name);
    Task HttpHandlerDeleted(string name);

    Task TcpHandlerCreated(string name);
    Task TcpHandlerDeleted(string name);

    Task SmbHandlerCreated(string name);
    Task SmbHandlerDeleted(string name);

    Task ExternalHandlerCreated(string name);
    Task ExternalHandlerDeleted(string name);

    #endregion

    #region HostedFiles

    Task HostedFileAdded(string id);
    Task HostedFileDeleted(string id);
    
    #endregion

    #region Drones

    Task NewDrone(string id);
    Task DroneCheckedIn(string id);
    Task DroneExited(string id);
    Task DroneDeleted(string id);
    Task DroneLost(string id);

    #endregion

    #region Tasks

    Task DroneTasked(string drone, string task);
    Task TaskUpdated(string drone, string task);
    Task TaskDeleted(string drone, string task);

    #endregion

    #region Events

    Task NewEvent(EventType type, string id);

    #endregion

    #region ReversePortForwards

    Task ReversePortForwardCreated(string id);
    Task ReversePortForwardDeleted(string id);

    #endregion
    
    # region Socks

    Task SocksProxyStarted(string id);
    Task SocksProxyStopped(string id);

    #endregion

    #region Webhooks

    Task WebhookCreated(string name);
    Task WebhookDeleted(string name);

    #endregion
}