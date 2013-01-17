﻿using System;
using System.Data;
using Nohros.Logging;
using Nohros.Resources;

namespace Nohros.Data.Providers
{
  /// <summary>
  /// Represents a transaction.
  /// </summary>
  public class Transaction : ITransaction, IDisposable
  {
    const string kClassName = "Nohros.Data.Providers.Transaction";

    readonly InternalTransaction internal_transaction_;
    readonly MustLogger logger_;
    bool complete_;

    #region .ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/>
    /// class by using the specified <see cref="IConnectionProvider"/> object
    /// and <see cref="TransactionBehavior"/>.
    /// </summary>
    /// <param name="connection">
    /// A <see cref="IDbConnection"/> that provide access to the
    /// underlying data provider.
    /// </param>
    public Transaction(IDbConnection connection) {
      if (connection == null) {
        throw new ArgumentNullException("connection");
      }
      logger_ = MustLogger.ForCurrentProcess;
      complete_ = false;

      internal_transaction_ = InternalTransaction.Get(connection);
    }
    #endregion

    public virtual void Dispose() {
      if (TransactionContext.Current == null) {
        internal_transaction_.Dispose();
      }
    }

    /// <summary>
    /// Attempts to commit the transaction.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The transaction is already completed.
    /// </exception>
    public virtual void Commit() {
      if (TransactionContext.Current == null) {
        internal_transaction_.Commit();
      }
      complete_ = true;
    }

    /// <summary>
    /// Rolls back(aborts) the transaction.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The transaction was already completed.
    /// </exception>
    public virtual void Rollback() {
      if (complete_) {
        throw new InvalidOperationException(
          Resources.Resources.DataProvider_InvalidOperation_TransactionCompleted);
      }
      internal_transaction_.Rollback();
      complete_ = true;
    }

    /// <summary>
    /// Executes the <see cref="IDbCommand.ExecuteScalar"/> against the
    /// specified <paramref name="cmd"/> object under the current transaction.
    /// </summary>
    /// <param name="cmd">
    /// The <see cref="IDbCommand"/> object that should be used to execute the
    /// scalar query.
    /// </param>
    /// <returns>
    /// The result of execution of <see cref="IDbCommand.ExecuteScalar"/> 
    /// against the <paramref name="cmd"/> object.
    /// </returns>
    public virtual object ExecuteScalar(IDbCommand cmd) {
      return internal_transaction_.ExecuteScalar(cmd);
    }

    /// <summary>
    /// Executes the <see cref="IDbCommand.ExecuteNonQuery"/> against the
    /// specified <paramref name="cmd"/> object under the current transaction.
    /// </summary>
    /// <param name="cmd">
    /// The <see cref="IDbCommand"/> object that should be used to execute the
    /// scalar query.
    /// </param>
    /// <returns>
    /// The result of execution of <see cref="IDbCommand.ExecuteNonQuery"/> 
    /// against the <paramref name="cmd"/> object.
    /// </returns>
    public virtual int ExecuteNonQuery(IDbCommand cmd) {
      return internal_transaction_.ExecuteNonQuery(cmd);
    }

    /// <summary>
    /// Executes the <see cref="IDbCommand.ExecuteReader()"/> against the
    /// specified <paramref name="cmd"/> object under the current transaction.
    /// </summary>
    /// <param name="cmd">
    /// The <see cref="IDbCommand"/> object that should be used to execute the
    /// scalar query.
    /// </param>
    /// <returns>
    /// The result of execution of <see cref="IDbCommand.ExecuteReader()"/> 
    /// against the <paramref name="cmd"/> object.
    /// </returns>
    public virtual IDataReader ExecuteReader(IDbCommand cmd) {
      return internal_transaction_.ExecuteReader(cmd);
    }

    /// <summary>
    /// Executes the <see cref="IDbCommand.ExecuteReader()"/> against the
    /// specified <paramref name="cmd"/> object under the current transaction
    /// and using the specified <see cref="CommandBehavior"/>.
    /// </summary>
    /// <param name="cmd">
    /// The <see cref="IDbCommand"/> object that should be used to execute the
    /// scalar query.
    /// </param>
    /// <param name="behavior">
    /// On of the <see cref="CommandBehavior"/> values.
    /// </param>
    /// <returns>
    /// The result of execution of <see cref="IDbCommand.ExecuteReader()"/> 
    /// against the <paramref name="cmd"/> object.
    /// </returns>
    public virtual IDataReader ExecuteReader(IDbCommand cmd,
      CommandBehavior behavior) {
      return internal_transaction_.ExecuteReader(cmd, behavior);
    }

    public CommandBuilder GetCommandBuilder() {
      return internal_transaction_.GetCommandBuilder();
    }

    /// <summary>
    /// Creates a dependent clone of the transaction.
    /// </summary>
    /// <returns>
    /// A <see cref="DependentTransaction"/> that represents the dependent
    /// clone.
    /// </returns>
    /// <remarks>
    /// A dependent transaction is a transaction whose outcome depends on the
    /// outcome of the transaction from which it was cloned.
    /// </remarks>
    public DependentTransaction DependentClone() {
      return new DependentTransaction(this);
    }
  }
}
