﻿namespace DotNetTypeGenerator.Delegates;

public class DelegateCache
{
    public static Dictionary<Guid, MulticastDelegate> Cache = new();

    public static Guid Add(MulticastDelegate multicastDelegate)
    {
        var id = Guid.NewGuid();
        Cache.Add(id, multicastDelegate);

        return id;
    }

    public static MulticastDelegate Get(Guid id)
    {
        return Cache[id];
    }
}
