using System;
using System.Collections.Generic;
using System.Text;

namespace OrbitBubble.Core.Common;

public readonly struct Result<T> {
  public bool IsSuccess { get; }
  public T? Value { get; }
  public StoreError? Error { get; }

  private Result(bool ok, T? value, StoreError? error) { IsSuccess = ok; Value = value; Error = error; }

  public static Result<T> Ok(T value) => new(true, value, null);
  public static Result<T> Fail(StoreError error) => new(false, default, error);
}

public sealed record StoreError(string Code, string Message, Exception? Exception = null);