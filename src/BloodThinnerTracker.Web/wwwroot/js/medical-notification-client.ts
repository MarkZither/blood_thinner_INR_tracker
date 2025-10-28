/**
 * BloodThinnerTracker.Web - SignalR Client for Medical Notifications
 * Licensed under MIT License. See LICENSE file in the project root.
 * 
 * TypeScript client for connecting to the medical notification SignalR hub.
 * Provides real-time medication reminders, INR alerts, and cross-device sync.
 * 
 * ⚠️ MEDICAL NOTIFICATION CLIENT:
 * This client handles real-time medical notifications. Ensure proper error
 * handling and fallback mechanisms for critical medical reminders.
 * 
 * IMPORTANT MEDICAL DISCLAIMER:
 * This is a supplementary notification system. Users should not rely solely
 * on electronic reminders for critical medical decisions.
 */

import * as signalR from '@microsoft/signalr';

/**
 * Interface for medication reminder data.
 */
export interface MedicationReminder {
    id: string;
    type: 'MedicationReminder';
    medicationId: string;
    medicationName: string;
    dosage: number;
    dosageUnit: string;
    scheduledTime: Date;
    reminderTime: Date;
    priority: 'Normal' | 'High' | 'Critical';
    message: string;
    instructions?: string;
    safetyNote: string;
}

/**
 * Interface for INR test reminder data.
 */
export interface INRTestReminder {
    id: string;
    type: 'INRTestReminder';
    scheduleId: string;
    scheduledDate: Date;
    daysUntilTest: number;
    targetINRRange?: string;
    preferredLaboratory?: string;
    priority: 'Normal' | 'High' | 'Critical';
    message: string;
    instructions?: string;
    safetyNote: string;
}

/**
 * Interface for critical medical alerts.
 */
export interface CriticalAlert {
    id: string;
    type: 'CriticalMissedMedication' | 'OverdueINRTest' | 'DangerousINRValue';
    priority: 'Critical';
    message: string;
    safetyNote: string;
    [key: string]: any; // Additional properties based on alert type
}

/**
 * Interface for data synchronization notifications.
 */
export interface DataSyncNotification {
    dataTypes: string[];
    requestedBy: string;
    timestamp: Date;
}

/**
 * Interface for presence status updates.
 */
export interface PresenceStatus {
    userId: string;
    status: string;
    deviceType: string;
    timestamp: Date;
    connectionId: string;
}

/**
 * Configuration options for the medical notification client.
 */
export interface MedicalNotificationClientOptions {
    hubUrl: string;
    accessToken?: string;
    deviceId?: string;
    automaticReconnect?: boolean;
    reconnectIntervals?: number[];
    enableLogging?: boolean;
}

/**
 * Event handlers for medical notifications.
 */
export interface MedicalNotificationHandlers {
    onMedicationReminder?: (reminder: MedicationReminder) => void;
    onINRReminder?: (reminder: INRTestReminder) => void;
    onCriticalAlert?: (alert: CriticalAlert) => void;
    onDataSyncNotification?: (notification: DataSyncNotification) => void;
    onPresenceStatusUpdated?: (status: PresenceStatus) => void;
    onMedicationReminderAcknowledged?: (data: any) => void;
    onINRReminderAcknowledged?: (data: any) => void;
    onConnectionStateChanged?: (state: signalR.HubConnectionState) => void;
    onError?: (error: Error) => void;
}

/**
 * Client for managing real-time medical notifications via SignalR.
 */
export class MedicalNotificationClient {
    private connection: signalR.HubConnection;
    private readonly options: MedicalNotificationClientOptions;
    private readonly handlers: MedicalNotificationHandlers;
    private isConnected: boolean = false;
    private reconnectAttempts: number = 0;
    private maxReconnectAttempts: number = 10;

    /**
     * Creates a new instance of the MedicalNotificationClient.
     * @param options Configuration options for the client.
     * @param handlers Event handlers for notifications.
     */
    constructor(options: MedicalNotificationClientOptions, handlers: MedicalNotificationHandlers = {}) {
        this.options = options;
        this.handlers = handlers;

        // Build SignalR connection
        const connectionBuilder = new signalR.HubConnectionBuilder()
            .withUrl(options.hubUrl, {
                accessTokenFactory: () => options.accessToken || '',
                headers: options.deviceId ? { 'X-Device-ID': options.deviceId } : undefined
            });

        // Configure automatic reconnect
        if (options.automaticReconnect !== false) {
            connectionBuilder.withAutomaticReconnect(options.reconnectIntervals || [0, 2000, 10000, 30000]);
        }

        // Configure logging
        if (options.enableLogging) {
            connectionBuilder.configureLogging(signalR.LogLevel.Information);
        }

        this.connection = connectionBuilder.build();
        this.setupEventHandlers();
        this.setupConnectionEvents();
    }

    /**
     * Starts the connection to the medical notification hub.
     */
    public async start(): Promise<void> {
        try {
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;
            
            if (this.options.enableLogging) {
                console.log('Medical notification client connected');
            }
            
            this.handlers.onConnectionStateChanged?.(this.connection.state);
        } catch (error) {
            this.isConnected = false;
            const errorMessage = error instanceof Error ? error : new Error('Failed to start connection');
            
            if (this.options.enableLogging) {
                console.error('Failed to start medical notification client:', errorMessage);
            }
            
            this.handlers.onError?.(errorMessage);
            throw errorMessage;
        }
    }

    /**
     * Stops the connection to the medical notification hub.
     */
    public async stop(): Promise<void> {
        try {
            await this.connection.stop();
            this.isConnected = false;
            
            if (this.options.enableLogging) {
                console.log('Medical notification client disconnected');
            }
            
            this.handlers.onConnectionStateChanged?.(this.connection.state);
        } catch (error) {
            const errorMessage = error instanceof Error ? error : new Error('Failed to stop connection');
            this.handlers.onError?.(errorMessage);
            throw errorMessage;
        }
    }

    /**
     * Subscribes to medication reminders for specific medications.
     * @param medicationIds Array of medication IDs to subscribe to.
     */
    public async subscribeToMedicationReminders(medicationIds: string[]): Promise<void> {
        this.ensureConnected();
        
        try {
            await this.connection.invoke('SubscribeToMedicationReminders', medicationIds);
            
            if (this.options.enableLogging) {
                console.log('Subscribed to medication reminders:', medicationIds);
            }
        } catch (error) {
            const errorMessage = error instanceof Error ? error : new Error('Failed to subscribe to medication reminders');
            this.handlers.onError?.(errorMessage);
            throw errorMessage;
        }
    }

    /**
     * Unsubscribes from medication reminders.
     * @param medicationIds Array of medication IDs to unsubscribe from.
     */
    public async unsubscribeFromMedicationReminders(medicationIds: string[]): Promise<void> {
        this.ensureConnected();
        
        try {
            await this.connection.invoke('UnsubscribeFromMedicationReminders', medicationIds);
            
            if (this.options.enableLogging) {
                console.log('Unsubscribed from medication reminders:', medicationIds);
            }
        } catch (error) {
            const errorMessage = error instanceof Error ? error : new Error('Failed to unsubscribe from medication reminders');
            this.handlers.onError?.(errorMessage);
            throw errorMessage;
        }
    }

    /**
     * Subscribes to INR test reminders.
     */
    public async subscribeToINRReminders(): Promise<void> {
        this.ensureConnected();
        
        try {
            await this.connection.invoke('SubscribeToINRReminders');
            
            if (this.options.enableLogging) {
                console.log('Subscribed to INR reminders');
            }
        } catch (error) {
            const errorMessage = error instanceof Error ? error : new Error('Failed to subscribe to INR reminders');
            this.handlers.onError?.(errorMessage);
            throw errorMessage;
        }
    }

    /**
     * Unsubscribes from INR test reminders.
     */
    public async unsubscribeFromINRReminders(): Promise<void> {
        this.ensureConnected();
        
        try {
            await this.connection.invoke('UnsubscribeFromINRReminders');
            
            if (this.options.enableLogging) {
                console.log('Unsubscribed from INR reminders');
            }
        } catch (error) {
            const errorMessage = error instanceof Error ? error : new Error('Failed to unsubscribe from INR reminders');
            this.handlers.onError?.(errorMessage);
            throw errorMessage;
        }
    }

    /**
     * Updates the presence status for cross-device awareness.
     * @param status Current status (online, away, busy, etc.).
     * @param deviceType Type of device (mobile, web, desktop).
     */
    public async updatePresenceStatus(status: string, deviceType: string): Promise<void> {
        this.ensureConnected();
        
        try {
            await this.connection.invoke('UpdatePresenceStatus', status, deviceType);
            
            if (this.options.enableLogging) {
                console.log('Updated presence status:', { status, deviceType });
            }
        } catch (error) {
            const errorMessage = error instanceof Error ? error : new Error('Failed to update presence status');
            this.handlers.onError?.(errorMessage);
            throw errorMessage;
        }
    }

    /**
     * Acknowledges a medication reminder.
     * @param reminderId ID of the reminder being acknowledged.
     * @param medicationLogId Optional ID of the medication log entry.
     */
    public async acknowledgeMedicationReminder(reminderId: string, medicationLogId?: string): Promise<void> {
        this.ensureConnected();
        
        try {
            await this.connection.invoke('AcknowledgeMedicationReminder', reminderId, medicationLogId);
            
            if (this.options.enableLogging) {
                console.log('Acknowledged medication reminder:', { reminderId, medicationLogId });
            }
        } catch (error) {
            const errorMessage = error instanceof Error ? error : new Error('Failed to acknowledge medication reminder');
            this.handlers.onError?.(errorMessage);
            throw errorMessage;
        }
    }

    /**
     * Acknowledges an INR test reminder.
     * @param scheduleId ID of the INR schedule being acknowledged.
     * @param testId Optional ID of the INR test.
     */
    public async acknowledgeINRReminder(scheduleId: string, testId?: string): Promise<void> {
        this.ensureConnected();
        
        try {
            await this.connection.invoke('AcknowledgeINRReminder', scheduleId, testId);
            
            if (this.options.enableLogging) {
                console.log('Acknowledged INR reminder:', { scheduleId, testId });
            }
        } catch (error) {
            const errorMessage = error instanceof Error ? error : new Error('Failed to acknowledge INR reminder');
            this.handlers.onError?.(errorMessage);
            throw errorMessage;
        }
    }

    /**
     * Requests data synchronization across devices.
     * @param dataTypes Types of data to synchronize.
     */
    public async requestDataSync(dataTypes: string[]): Promise<void> {
        this.ensureConnected();
        
        try {
            await this.connection.invoke('RequestDataSync', dataTypes);
            
            if (this.options.enableLogging) {
                console.log('Requested data sync:', dataTypes);
            }
        } catch (error) {
            const errorMessage = error instanceof Error ? error : new Error('Failed to request data sync');
            this.handlers.onError?.(errorMessage);
            throw errorMessage;
        }
    }

    /**
     * Gets the current connection state.
     */
    public getConnectionState(): signalR.HubConnectionState {
        return this.connection.state;
    }

    /**
     * Gets whether the client is currently connected.
     */
    public get connected(): boolean {
        return this.isConnected && this.connection.state === signalR.HubConnectionState.Connected;
    }

    /**
     * Sets up event handlers for incoming notifications.
     */
    private setupEventHandlers(): void {
        // Medication reminder received
        this.connection.on('MedicationReminderReceived', (reminder: MedicationReminder) => {
            if (this.options.enableLogging) {
                console.log('Medication reminder received:', reminder);
            }
            this.handlers.onMedicationReminder?.(reminder);
        });

        // INR reminder received
        this.connection.on('INRReminderReceived', (reminder: INRTestReminder) => {
            if (this.options.enableLogging) {
                console.log('INR reminder received:', reminder);
            }
            this.handlers.onINRReminder?.(reminder);
        });

        // Critical alert received
        this.connection.on('CriticalAlertReceived', (alert: CriticalAlert) => {
            if (this.options.enableLogging) {
                console.warn('Critical alert received:', alert);
            }
            this.handlers.onCriticalAlert?.(alert);
        });

        // Data sync notification
        this.connection.on('DataSyncNotification', (notification: DataSyncNotification) => {
            if (this.options.enableLogging) {
                console.log('Data sync notification received:', notification);
            }
            this.handlers.onDataSyncNotification?.(notification);
        });

        // Presence status updated
        this.connection.on('PresenceStatusUpdated', (status: PresenceStatus) => {
            if (this.options.enableLogging) {
                console.log('Presence status updated:', status);
            }
            this.handlers.onPresenceStatusUpdated?.(status);
        });

        // Medication reminder acknowledged
        this.connection.on('MedicationReminderAcknowledged', (data: any) => {
            if (this.options.enableLogging) {
                console.log('Medication reminder acknowledged:', data);
            }
            this.handlers.onMedicationReminderAcknowledged?.(data);
        });

        // INR reminder acknowledged
        this.connection.on('INRReminderAcknowledged', (data: any) => {
            if (this.options.enableLogging) {
                console.log('INR reminder acknowledged:', data);
            }
            this.handlers.onINRReminderAcknowledged?.(data);
        });

        // Data sync requested
        this.connection.on('DataSyncRequested', (data: any) => {
            if (this.options.enableLogging) {
                console.log('Data sync requested:', data);
            }
            // Trigger local data sync based on requested data types
            this.handlers.onDataSyncNotification?.(data);
        });
    }

    /**
     * Sets up connection lifecycle event handlers.
     */
    private setupConnectionEvents(): void {
        this.connection.onclose((error) => {
            this.isConnected = false;
            
            if (this.options.enableLogging) {
                if (error) {
                    console.error('Medical notification client connection closed with error:', error);
                } else {
                    console.log('Medical notification client connection closed');
                }
            }
            
            this.handlers.onConnectionStateChanged?.(this.connection.state);
            
            if (error) {
                this.handlers.onError?.(error instanceof Error ? error : new Error(error.toString()));
            }
        });

        this.connection.onreconnecting((error) => {
            this.isConnected = false;
            this.reconnectAttempts++;
            
            if (this.options.enableLogging) {
                console.log(`Medical notification client reconnecting (attempt ${this.reconnectAttempts})...`, error);
            }
            
            this.handlers.onConnectionStateChanged?.(this.connection.state);
        });

        this.connection.onreconnected((connectionId) => {
            this.isConnected = true;
            this.reconnectAttempts = 0;
            
            if (this.options.enableLogging) {
                console.log('Medical notification client reconnected:', connectionId);
            }
            
            this.handlers.onConnectionStateChanged?.(this.connection.state);
        });
    }

    /**
     * Ensures the connection is active before invoking methods.
     */
    private ensureConnected(): void {
        if (!this.connected) {
            throw new Error('Medical notification client is not connected');
        }
    }
}

/**
 * Factory function for creating a medical notification client with common configurations.
 * @param baseUrl Base URL of the API.
 * @param accessToken JWT access token for authentication.
 * @param deviceId Optional device identifier.
 * @param handlers Event handlers for notifications.
 * @returns Configured MedicalNotificationClient instance.
 */
export function createMedicalNotificationClient(
    baseUrl: string,
    accessToken: string,
    deviceId?: string,
    handlers: MedicalNotificationHandlers = {}
): MedicalNotificationClient {
    const options: MedicalNotificationClientOptions = {
        hubUrl: `${baseUrl}/hubs/medical-notifications`,
        accessToken,
        deviceId,
        automaticReconnect: true,
        reconnectIntervals: [0, 2000, 10000, 30000, 60000],
        enableLogging: process.env.NODE_ENV === 'development'
    };

    return new MedicalNotificationClient(options, handlers);
}