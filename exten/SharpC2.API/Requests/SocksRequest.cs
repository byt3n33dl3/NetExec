﻿namespace SharpC2.API.Requests;

public sealed class SocksRequest
{
    public string DroneId { get; set; }
    public int BindPort { get; set; }
}