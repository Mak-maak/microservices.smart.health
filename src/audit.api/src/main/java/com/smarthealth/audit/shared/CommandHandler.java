package com.smarthealth.audit.shared;

public interface CommandHandler<C extends Command<R>, R> {
    R handle(C command);
}
