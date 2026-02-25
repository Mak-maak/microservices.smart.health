"""Initial migration - create prescriptions and ml_training_metadata tables

Revision ID: 001
Revises: 
Create Date: 2024-01-01 00:00:00.000000

"""
from alembic import op
import sqlalchemy as sa
from sqlalchemy.dialects import postgresql

revision = '001'
down_revision = None
branch_labels = None
depends_on = None


def upgrade() -> None:
    op.create_table(
        'prescriptions',
        sa.Column('id', postgresql.UUID(as_uuid=True), primary_key=True),
        sa.Column('appointment_id', postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column('patient_id', postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column('doctor_id', postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column('symptoms', postgresql.JSON(), nullable=False),
        sa.Column('diagnosis', sa.String(500), nullable=False),
        sa.Column('medications', postgresql.JSON(), nullable=False),
        sa.Column('dosage', postgresql.JSON(), nullable=False),
        sa.Column('notes', sa.Text(), nullable=True),
        sa.Column('created_at', sa.DateTime(), nullable=False),
        sa.Column('updated_at', sa.DateTime(), nullable=False),
        sa.Column('version', sa.Integer(), nullable=False, server_default='1'),
    )
    op.create_index('ix_prescriptions_appointment_id', 'prescriptions', ['appointment_id'])
    op.create_index('ix_prescriptions_patient_id', 'prescriptions', ['patient_id'])
    op.create_index('ix_prescriptions_doctor_id', 'prescriptions', ['doctor_id'])

    op.create_table(
        'ml_training_metadata',
        sa.Column('id', postgresql.UUID(as_uuid=True), primary_key=True),
        sa.Column('model_version', sa.String(50), nullable=False),
        sa.Column('trained_at', sa.DateTime(), nullable=False),
        sa.Column('accuracy_score', sa.Float(), nullable=True),
        sa.Column('dataset_size', sa.Integer(), nullable=False, server_default='0'),
    )


def downgrade() -> None:
    op.drop_table('ml_training_metadata')
    op.drop_index('ix_prescriptions_doctor_id', 'prescriptions')
    op.drop_index('ix_prescriptions_patient_id', 'prescriptions')
    op.drop_index('ix_prescriptions_appointment_id', 'prescriptions')
    op.drop_table('prescriptions')
