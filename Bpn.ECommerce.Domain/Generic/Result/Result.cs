﻿using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bpn.ECommerce.Domain.Generic.Result
{

    public sealed class Result<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }
        [JsonPropertyName("errorMessages")]
        public List<string>? ErrorMessages { get; set; }
        [JsonPropertyName("isSuccessful")]
        public bool IsSuccessful { get; set; } = true;

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

        [JsonConstructor]
        public Result()
        {
        }

        public Result(T data)
        {
            Data = data;
        }

        public Result(int statusCode, List<string> errorMessages)
        {
            IsSuccessful = false;
            StatusCode = statusCode;
            ErrorMessages = errorMessages;
        }

        public Result(int statusCode, string errorMessage)
        {
            IsSuccessful = false;
            StatusCode = statusCode;
            ErrorMessages = new() { errorMessage };
        }

        public static implicit operator Result<T>(T data)
        {
            return new(data);
        }

        public static implicit operator Result<T>((int statusCode, List<string> errorMessages) parameters)
        {
            return new(parameters.statusCode, parameters.errorMessages);
        }

        public static implicit operator Result<T>((int statusCode, string errorMessage) parameters)
        {
            return new(parameters.statusCode, parameters.errorMessage);
        }
        public static Result<T> Succeed(T data)
        {
            return new(data);
        }

        public static Result<T> Failure(int statusCode, List<string> errorMessages)
        {
            return new(statusCode, errorMessages);
        }

        public static Result<T> Failure(int statusCode, string errorMessage)
        {
            return new(statusCode, errorMessage);
        }

        public static Result<T> Failure(string errorMessage)
        {
            return new(500, errorMessage);
        }

        public static Result<T> Failure(List<string> errorMessages)
        {
            return new(500, errorMessages);
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
