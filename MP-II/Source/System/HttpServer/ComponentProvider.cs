using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace HttpServer
{
  internal class ComponentProvider : IComponentProvider
  {
    private readonly Dictionary<Type, TypeInformation> _instances = new Dictionary<Type, TypeInformation>();

    /// <summary>
    /// Add a component instance
    /// </summary>
    /// <typeparam name="T">Interface type</typeparam>
    /// <param name="instance">Instance to add</param>
    public void AddInstance<T>(object instance)
    {
      Type interfaceType = typeof(T);
      lock (_instances)
        _instances.Add(
            interfaceType,
            new TypeInformation
              {
                  InstanceType = instance.GetType(),
                  Instance = instance,
                  InterfaceType = interfaceType
              });
    }

    /// <summary>
    /// Get a component.
    /// </summary>
    /// <typeparam name="T">Interface type</typeparam>
    /// <returns>Component if registered, otherwise null.</returns>
    /// <remarks>
    /// Component will get created if needed.
    /// </remarks>
    public T Get<T>() where T : class
    {
      Type type = typeof(T);
      TypeInformation typeInformation;
      lock (_instances)
        if (!_instances.TryGetValue(type, out typeInformation))
          return null;

      if (typeInformation.Instance != null)
        return (T) typeInformation.Instance;

      return (T) Create(typeInformation);
    }

    private class ConstructorParameter
    {
      public object Instance { get; set; }
      public ParameterInfo Parameter { get; set; }
    }

    /// <exception cref="InvalidOperationException">If instance cannot be created.</exception>
    private object Create(TypeInformation information)
    {
      Dictionary<ConstructorInfo, List<ConstructorParameter>> constructors =
          new Dictionary<ConstructorInfo, List<ConstructorParameter>>();
      ConstructorInfo[] publicConstructors = information.InstanceType.GetConstructors();
      if (publicConstructors.Length == 0)
        throw new InvalidOperationException(information.InstanceType.FullName + " do not have any public constructors.");

      foreach (var constructor in publicConstructors)
      {
        constructors.Add(constructor, new List<ConstructorParameter>());
        foreach (var parameter in constructor.GetParameters())
        {
          ConstructorParameter constructorParameter = new ConstructorParameter {Parameter = parameter};
          constructors[constructor].Add(constructorParameter);

          TypeInformation typeInfo;
          lock (_instances)
            if (!_instances.TryGetValue(parameter.ParameterType, out typeInfo))
              continue; // this constructor wont work, but check what parameters we are missing.

          try
          {
            constructorParameter.Instance = typeInfo.Instance ?? Create(typeInfo);
          }
          catch (InvalidOperationException err)
          {
            throw new InvalidOperationException(
                "Failed to create '" + typeInfo.InterfaceType.FullName + "' that '" + information.InterfaceType +
                    "' is dependent off. Check inner exception for more details.", err);
          }
        }

        // check if all parameters was found.
        bool allFound = true;
        foreach (var parameter in constructors[constructor])
        {
          if (parameter.Instance != null) continue;
          allFound = false;
          break;
        }

        if (!allFound)
          continue;

        // now create instance.
        information.ConstructorArguments = new object[constructors[constructor].Count];
        int index = 0;
        foreach (var parameter in constructors[constructor])
          information.ConstructorArguments[index++] = parameter.Instance;
        return Activator.CreateInstance(information.InstanceType, information.ConstructorArguments);
      }

      StringBuilder sb = new StringBuilder();
      sb.AppendLine(
          "Failed to create '" + information.InstanceType.FullName + "', due to missing constructorparamters.");
      foreach (var pair in constructors)
      {
        sb.Append(pair.Key + " are missing: ");
        foreach (var parameter in pair.Value)
        {
          if (parameter.Instance == null)
            sb.Append(parameter.Parameter.Name + ", ");
        }
        sb.Length -= 2;
        sb.AppendLine();
      }
      throw new InvalidOperationException(sb.ToString());
    }

    /// <summary>
    /// Checks if the specified component interface have been added.
    /// </summary>
    /// <param name="interfaceType"></param>
    /// <returns>true if found; otherwise false.</returns>
    public bool Contains(Type interfaceType)
    {
      return _instances.ContainsKey(interfaceType);
    }

    /// <summary>
    /// Add a component.
    /// </summary>
    /// <typeparam name="InterfaceType">Type being requested.</typeparam>
    /// <typeparam name="InstanceType">Type being created.</typeparam>
    /// <exception cref="InvalidOperationException">Type have already been mapped.</exception>
    public void Add<InterfaceType, InstanceType>()
    {
      Type interfaceType = typeof(InterfaceType);
      TypeInformation typeInformation = new TypeInformation()
        {
            InstanceType = typeof(InstanceType),
            InterfaceType = interfaceType,
            Instance = null
        };
      lock (_instances)
      {
        if (_instances.ContainsKey(interfaceType))
          throw new InvalidOperationException(
              "Type '" + interfaceType + "' have already been mapped to '" +
                  typeof(InstanceType).FullName);
        _instances.Add(interfaceType, typeInformation);
      }
    }

    private class TypeInformation
    {
      public Type InterfaceType { get; set; }
      public Type InstanceType { get; set; }
      public object Instance { get; set; }
      public object[] ConstructorArguments { get; set; }
    }
  }
}