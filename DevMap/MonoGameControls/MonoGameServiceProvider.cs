using System;
using System.Collections.Generic;

namespace DevMap.MonoGameControls;

public class MonoGameServiceProvider : IServiceProvider
{
	private readonly Dictionary<Type, object> _services = [];

	public void AddService(Type type, object provider)
	{
		_services.Add(type, provider);
	}

	public object? GetService(Type type)
	{
		if (_services.TryGetValue(type, out var service))
			return service;
		return null;
	}

	public void RemoveService(Type type)
	{
		_services.Remove(type);
	}

	public void AddService<T>(T service) where T : notnull
	{
		AddService(typeof(T), service);
	}

	public T GetService<T>() where T : class
	{
		var service = GetService(typeof(T));
		return (T)service!;
	}
}
