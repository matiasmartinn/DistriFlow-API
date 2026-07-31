namespace DistriFlow.Application.Common.Results;

public static class IdempotencyErrors
{
    public static Error Conflict()
    {
        return Error.Conflict(
            "idempotency.key_conflict",
            "Idempotency-Key ya fue utilizada para otra operacion o recurso.");
    }

    public static Error InvalidKey(string message)
    {
        return Error.Validation("idempotency.invalid_key", message);
    }
}
