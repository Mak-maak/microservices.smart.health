package com.smarthealth.audit.infrastructure;

import org.springframework.stereotype.Service;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.time.Instant;
import java.util.HexFormat;
import java.util.UUID;

@Service
public class HashService {

    public String computeHash(
            String previousHash,
            UUID eventId,
            String eventType,
            String aggregateId,
            Instant occurredAt
    ) {
        String input = previousHash + "|" + eventId + "|" + eventType + "|" + aggregateId + "|" + occurredAt;
        try {
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hash = digest.digest(input.getBytes(StandardCharsets.UTF_8));
            return HexFormat.of().formatHex(hash);
        } catch (NoSuchAlgorithmException e) {
            throw new IllegalStateException("SHA-256 algorithm not available", e);
        }
    }
}
