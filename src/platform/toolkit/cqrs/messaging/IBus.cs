﻿using System;

namespace Nohros.CRQS.Messaging
{
  public interface IBus : IPublisher
  {
    /// <summary>
    /// Sends a message to a registered handler.
    /// </summary>
    /// <typeparam name="T">
    /// The type of message to be sent.
    /// </typeparam>
    /// <param name="msg">
    /// The message to sent.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// There are more than one handler or no handler that is capable
    /// to handle the message is registered.
    /// </exception>
    void Send<T>(T msg) where T : IMessage;
  }
}
