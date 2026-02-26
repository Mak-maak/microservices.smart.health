export const configuration = () => ({
  port: parseInt(process.env.PORT, 10) || 3003,
  nodeEnv: process.env.NODE_ENV || 'development',
  cosmosDb: {
    endpoint: process.env.COSMOS_DB_ENDPOINT || '',
    key: process.env.COSMOS_DB_KEY || '',
    database: process.env.COSMOS_DB_DATABASE || 'eventrecorder-db',
    container: process.env.COSMOS_DB_CONTAINER || 'event-records',
  },
  serviceBus: {
    connectionString: process.env.AZURE_SERVICE_BUS_CONNECTION_STRING || '',
    topics: {
      appointmentCreated: process.env.TOPIC_APPOINTMENT_CREATED || 'appointment-created',
      paymentCompleted: process.env.TOPIC_PAYMENT_COMPLETED || 'payment-completed',
      paymentFailed: process.env.TOPIC_PAYMENT_FAILED || 'payment-failed',
      prescriptionCreated: process.env.TOPIC_PRESCRIPTION_CREATED || 'prescription-created',
      shipmentDispatched: process.env.TOPIC_SHIPMENT_DISPATCHED || 'shipment-dispatched',
    },
    subscriptionName: process.env.SUBSCRIPTION_NAME || 'eventrecorder-service',
  },
});
