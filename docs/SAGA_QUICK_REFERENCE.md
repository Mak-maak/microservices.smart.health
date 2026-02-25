# 🚀 Saga Quick Reference

## API Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/patients/Create` | Create patient |
| POST | `/api/doctors/Create` | Create doctor |
| POST | `/api/appointments/Book` | Book appointment (starts saga) |
| GET | `/api/appointments/{id}` | Get appointment |
| DELETE | `/api/appointments/{id}?reason=...` | Cancel appointment |

## Saga States

```
Initial → Validating → Reserving → Confirming → Completed ✅
             ↓            ↓            ↓
          Compensating → Failed ❌
```

## Consumers

1. **ValidateDoctorAvailabilityConsumer** - Checks doctor conflicts
2. **ReserveSlotConsumer** - Reserves appointment slot  
3. **ConfirmAppointmentCommandConsumer** - Confirms appointment
4. **CompensateAppointmentConsumer** - Handles rollback

## Quick Test

```powershell
# Create patient & doctor (save IDs)
# Book appointment
$appointment = @{
    patientId='<PATIENT-ID>'
    doctorId='<DOCTOR-ID>'
    startTime='2026-03-01T10:00:00Z'
    endTime='2026-03-01T10:30:00Z'
    reason='Test'
} | ConvertTo-Json

Invoke-RestMethod -Uri 'http://localhost:8080/api/appointments/Book' `
    -Method POST -Body $appointment -ContentType 'application/json'

# Wait 15 seconds, check saga state
docker exec microservicessmarthealth-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd `
  -S localhost -U sa -P 'YourStrong!Passw0rd' -C `
  -Q "SELECT CorrelationId, CurrentState FROM SmartHealthAppointments.dbo.AppointmentSagaStates"
```

## Monitoring

```bash
# View logs
docker compose logs api --tail=50

# Check saga states
docker exec microservicessmarthealth-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong!Passw0rd' -C \
  -Q "SELECT * FROM SmartHealthAppointments.dbo.AppointmentSagaStates"

# Check outbox messages
docker exec microservicessmarthealth-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong!Passw0rd' -C \
  -Q "SELECT TOP 10 MessageType, ProcessedAt FROM SmartHealthAppointments.dbo.OutboxMessages ORDER BY CreatedAt DESC"
```

## Architecture

```
Client → API → Domain → DbContext → OutboxMessages
                                        ↓
                                   OutboxPublisher
                                        ↓
                                  MassTransit Bus
                                        ↓
                        ┌───────────────┼───────────────┐
                        ↓               ↓               ↓
                      Saga         Consumers      Integration Events
                        ↓
                AppointmentSagaStates (DB)
```

## Key Files

- **Saga:** `Infrastructure/Saga/AppointmentBookingSaga.cs`
- **Consumers:** `Infrastructure/Saga/Consumers/*.cs`
- **Messages:** `Infrastructure/Messaging/IntegrationMessages.cs`
- **DbContext:** `Infrastructure/Persistence/AppointmentsDbContext.cs`
- **Config:** `Program.cs` (MassTransit section)
