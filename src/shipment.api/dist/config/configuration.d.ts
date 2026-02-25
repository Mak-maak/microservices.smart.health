export declare const configuration: () => {
    port: number;
    nodeEnv: string;
    database: {
        url: string;
    };
    serviceBus: {
        connectionString: string;
        topics: {
            prescriptionCreated: string;
            medicinesPrescribed: string;
            shipmentCreated: string;
            shipmentDispatched: string;
            shipmentDelivered: string;
            shipmentFailed: string;
            auditEvents: string;
        };
        subscriptionName: string;
    };
};
