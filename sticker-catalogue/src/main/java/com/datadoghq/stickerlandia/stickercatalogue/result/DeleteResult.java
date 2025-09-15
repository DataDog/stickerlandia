package com.datadoghq.stickerlandia.stickercatalogue.result;

/** Result type for delete operations. */
public sealed interface DeleteResult permits DeleteResult.Success, DeleteResult.NotFound {

    record Success() implements DeleteResult {}

    record NotFound(String stickerId) implements DeleteResult {}
}
