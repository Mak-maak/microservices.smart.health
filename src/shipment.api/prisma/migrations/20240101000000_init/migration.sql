-- CreateTable
CREATE TABLE "Shipment" (
    "id" TEXT NOT NULL,
    "prescriptionId" TEXT NOT NULL,
    "patientId" TEXT NOT NULL,
    "pharmacyId" TEXT NOT NULL,
    "medications" JSONB NOT NULL,
    "shipmentStatus" TEXT NOT NULL DEFAULT 'CREATED',
    "trackingNumber" TEXT,
    "address" JSONB NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,
    "version" INTEGER NOT NULL DEFAULT 0,

    CONSTRAINT "Shipment_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "ProcessedMessage" (
    "id" TEXT NOT NULL,
    "shipmentId" TEXT,
    "processedAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "ProcessedMessage_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE INDEX "Shipment_prescriptionId_idx" ON "Shipment"("prescriptionId");

-- CreateIndex
CREATE INDEX "Shipment_patientId_idx" ON "Shipment"("patientId");

-- AddForeignKey
ALTER TABLE "ProcessedMessage" ADD CONSTRAINT "ProcessedMessage_shipmentId_fkey" FOREIGN KEY ("shipmentId") REFERENCES "Shipment"("id") ON DELETE SET NULL ON UPDATE CASCADE;
