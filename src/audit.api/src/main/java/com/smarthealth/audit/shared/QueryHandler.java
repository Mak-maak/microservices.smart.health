package com.smarthealth.audit.shared;

public interface QueryHandler<Q extends Query<R>, R> {
    R handle(Q query);
}
