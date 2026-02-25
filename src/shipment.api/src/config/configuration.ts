export const configuration = () => ({
  port: parseInt(process.env.PORT, 10) || 3000,
  nodeEnv: process.env.NODE_ENV || 'development',
  database: {
    url: process.env.DATABASE_URL,
  },
  serviceBus: {
    connectionString: process.env.AZURE_SERVICE_BUS_CONNECTION_STRING || '',
    topics: {
      prescriptionCreated: process.env.TOPIC_PRESCRIPTION_CREATED || 'prescription-created',
      medicinesPrescribed: process.env.TOPIC_MEDICINES_PRESCRIBED || 'medicines-prescribed',
      shipmentCreated: process.env.TOPIC_SHIPMENT_CREATED || 'shipment-created',
      shipmentDispatched: process.env.TOPIC_SHIPMENT_DISPATCHED || 'shipment-dispatched',
      shipmentDelivered: process.env.TOPIC_SHIPMENT_DELIVERED || 'shipment-delivered',
      shipmentFailed: process.env.TOPIC_SHIPMENT_FAILED || 'shipment-failed',
      auditEvents: process.env.TOPIC_AUDIT_EVENTS || 'audit-events',
    },
    subscriptionName: process.env.SUBSCRIPTION_NAME || 'shipment-service',
  },
});
