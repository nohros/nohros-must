﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Nohros.Concurrent
{
  /// <summary>
  /// Mailbox is basically a queue to store messages sent to a object that
  /// lives in a particular thread.
  /// </summary>
  /// <remarks> Mailbox is intended to be used on concurrent enviroments that
  /// use message passing to achieve concurrency and internl scalability.
  /// <para>This mode of concurrency allows multithreaded applications to
  /// work without using mutexes, condition variables or semaphores to
  /// orchestrate the parallel processing. Instead, each object can live in
  /// its own thread and no other thread should ever touch it(that's why
  /// mutexes are not needed). Other threads can comunnicate with the object
  /// by sendind it messages(T). Same way the object can speak to other
  /// objects - potentially running in different threads - by sending them
  /// messages.
  /// </para>
  /// </remarks>
  /// <typeparam name="T">The type of the messages that the mailbox can
  /// receive.
  /// </typeparam>
  public class Mailbox<T>
  {
    // The pipe to store actual messages
    YQueue<T> message_queue_;

    // There is only one thread receiving from the mailbox, but there is
    // arbitrary number of threads sending. Given that |pipe| requires
    // synchronized access on both of its endpoints, we have to synchronize
    // the sending side.
    object mutex_ ;

    // True if the underlying queue is active, ie. when we are allowed to
    // read command from it.
    bool active_;

    // Signaler to pass signals from writer thread to reader thread.
    ManualResetEvent signaler_;
 
    #region .ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="Mailbox{T}"/> class.
    /// </summary>
    public Mailbox() {
      mutex_ = new object();
      signaler_ = new ManualResetEvent(false);
      message_queue_ = new YQueue<T>(0);
    }
    #endregion

    /// <summary>
    /// Sends a command to the mailbox.
    /// </summary>
    /// <param name="message"></param>
    public void Send(T message) {
      lock(mutex_) {
        message_queue_.Enqueue(message);
      }
    }

    /// <summary>
    /// Receives a command from the mailbox.
    /// </summary>
    /// <param name="message">The message that was received.</param>
    /// <param name="timeout_ms">A timeout </param>
    /// <returns></returns>
    public bool Receive(out T message, int timeout_ms) {
      bool ok;
      // try to get command straight away.
      if(active_) {
        ok = message_queue_.Dequeue(out message);
        if(ok) {
          return true;
        }

        // If there are no more messages available, switch into passive mode.
        active_ = false;
      }

      // Wait for signal from the message sender.
      bool signaled = signaler_.WaitOne(timeout_ms);
      if (!signaled) {
        message = default(T);
        return false;
      }

      // We've got a signal. Now we can switch into the active state.
      active_ = true;

      // Get a message
      ok = message_queue_.Dequeue(out message);
#if DEBUG
      if(!ok)
        throw new Exception("A signal was received from the sender thread. The queue should not be empty here.");
#endif
      return ok;
    }
  }
}