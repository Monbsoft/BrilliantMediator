using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrilliantMediator.Abstractions.Commands;

/// <summary>
/// Represents a command that does not return a response.
/// </summary>
public interface ICommand { }

/// <summary>
/// Represents a command that returns a response of type TResponse.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface ICommand<out TResponse> { }
