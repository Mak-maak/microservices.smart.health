using MassTransit;
using SmartHealth.Appointments.Infrastructure.Messaging;

namespace SmartHealth.Appointments.Infrastructure.Saga;

/// <summary>
/// Orchestration-based Saga (State Machine) for appointment booking.
///
/// Flow:
///   AppointmentRequested
///     → ValidateDoctorAvailability
///     → ReserveSlot
///     → ConfirmAppointment
///     → Publish AppointmentConfirmed
///
/// On failure:
///     → Compensate
///     → Publish AppointmentFailed
/// </summary>
public sealed class AppointmentBookingSaga : MassTransitStateMachine<AppointmentSagaState>
{
    // States
    public State Validating { get; private set; } = null!;
    public State Reserving { get; private set; } = null!;
    public State Confirming { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Compensating { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    // Events
    public Event<AppointmentRequestedMessage> AppointmentRequested { get; private set; } = null!;
    public Event<DoctorAvailabilityValidatedMessage> DoctorAvailabilityValidated { get; private set; } = null!;
    public Event<DoctorUnavailableMessage> DoctorUnavailable { get; private set; } = null!;
    public Event<SlotReservedMessage> SlotReserved { get; private set; } = null!;
    public Event<SlotReservationFailedMessage> SlotReservationFailed { get; private set; } = null!;
    public Event<AppointmentConfirmedMessage> AppointmentConfirmed { get; private set; } = null!;

    public AppointmentBookingSaga()
    {
        // The CorrelationId in all messages maps to the AppointmentId
        InstanceState(x => x.CurrentState);
        ConfigureCorrelationIds();
        ConfigureTransitions();
    }

    private void ConfigureCorrelationIds()
    {
        Event(() => AppointmentRequested, x =>
            x.CorrelateById(ctx => ctx.Message.AppointmentId));

        Event(() => DoctorAvailabilityValidated, x =>
            x.CorrelateById(ctx => ctx.Message.AppointmentId));

        Event(() => DoctorUnavailable, x =>
            x.CorrelateById(ctx => ctx.Message.AppointmentId));

        Event(() => SlotReserved, x =>
            x.CorrelateById(ctx => ctx.Message.AppointmentId));

        Event(() => SlotReservationFailed, x =>
            x.CorrelateById(ctx => ctx.Message.AppointmentId));

        Event(() => AppointmentConfirmed, x =>
            x.CorrelateById(ctx => ctx.Message.AppointmentId));
    }

    private void ConfigureTransitions()
    {
        // Step 1 – Request received: initiate availability check
        Initially(
            When(AppointmentRequested)
                .Then(ctx =>
                {
                    ctx.Saga.PatientId = ctx.Message.PatientId;
                    ctx.Saga.DoctorId = ctx.Message.DoctorId;
                    ctx.Saga.SlotStartTime = ctx.Message.StartTime;
                    ctx.Saga.SlotEndTime = ctx.Message.EndTime;
                })
                .Publish(ctx => new ValidateDoctorAvailabilityCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.DoctorId,
                    ctx.Saga.SlotStartTime,
                    ctx.Saga.SlotEndTime))
                .TransitionTo(Validating));

        // Step 2 – Doctor available: reserve the slot
        During(Validating,
            When(DoctorAvailabilityValidated)
                .Publish(ctx => new ReserveSlotCommand(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.DoctorId,
                    ctx.Saga.SlotStartTime,
                    ctx.Saga.SlotEndTime))
                .TransitionTo(Reserving),

            When(DoctorUnavailable)
                .Publish(ctx => new CompensateAppointmentCommand(
                    ctx.Saga.CorrelationId,
                    "Doctor is not available at the requested time."))
                .TransitionTo(Compensating));

        // Step 3 – Slot reserved: confirm appointment
        During(Reserving,
            When(SlotReserved)
                .Publish(ctx => new ConfirmAppointmentCommand(ctx.Saga.CorrelationId))
                .TransitionTo(Confirming),

            When(SlotReservationFailed)
                .Publish(ctx => new CompensateAppointmentCommand(
                    ctx.Saga.CorrelationId,
                    "Slot reservation failed."))
                .TransitionTo(Compensating));

        // Step 4 – Confirmed: saga complete
        During(Confirming,
            When(AppointmentConfirmed)
                .Publish(ctx => new AppointmentConfirmedIntegrationEvent(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.PatientId,
                    ctx.Saga.DoctorId))
                .TransitionTo(Completed)
                .Finalize());

        // Compensation: publish failure event and finalize
        During(Compensating,
            Ignore(DoctorUnavailable),
            Ignore(SlotReservationFailed));

        SetCompletedWhenFinalized();
    }
}
