﻿using System;

namespace MessagingService.SMSMessageAggregate
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Shared.DomainDrivenDesign.EventSourcing;
    using Shared.EventStore.EventStore;
    using Shared.General;
    using SMSMessage.DomainEvents;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Shared.EventStore.EventStore.Aggregate" />
    public class SMSAggregate : Aggregate
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SMSAggregate" /> class.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public SMSAggregate()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SMSAggregate" /> class.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        private SMSAggregate(Guid aggregateId)
        {
            Guard.ThrowIfInvalidGuid(aggregateId, "Aggregate Id cannot be an Empty Guid");

            this.AggregateId = aggregateId;
            this.MessageId = aggregateId;
        }

        #endregion

        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        public Guid MessageId { get; private set; }
        /// <summary>
        /// Gets the sender.
        /// </summary>
        /// <value>
        /// The sender.
        /// </value>
        public String Sender { get; private set; }
        /// <summary>
        /// Gets the destination.
        /// </summary>
        /// <value>
        /// The destination.
        /// </value>
        public String Destination { get; private set; }
        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public String Message { get; private set; }
        /// <summary>
        /// Gets the provider reference.
        /// </summary>
        /// <value>
        /// The provider reference.
        /// </value>
        public String ProviderReference { get; private set; }

        /// <summary>
        /// Gets the message status.
        /// </summary>
        /// <value>
        /// The message status.
        /// </value>
        public MessageStatus MessageStatus { get; private set; }

        /// <summary>
        /// Creates the specified aggregate identifier.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <returns></returns>
        public static SMSAggregate Create(Guid aggregateId)
        {
            return new SMSAggregate(aggregateId);
        }

        /// <summary>
        /// Messages the send to recipient failure.
        /// </summary>
        /// <param name="providerSMSReference">The provider SMS reference.</param>
        public void ReceiveResponseFromProvider(String providerSMSReference)
        {
            ResponseReceivedFromProviderEvent responseReceivedFromProviderEvent =
                ResponseReceivedFromProviderEvent.Create(this.AggregateId, providerSMSReference);

            this.ApplyAndPend(responseReceivedFromProviderEvent);
        }

        /// <summary>
        /// Sends the request to provider.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="System.InvalidOperationException">Cannot send a message to provider that has already been sent</exception>
        public void SendRequestToProvider(String sender,
                                          String destination,
                                          String message)
        {
            if (this.MessageStatus != MessageStatus.NotSet)
            {
                throw new InvalidOperationException("Cannot send a message to provider that has already been sent");
            }

            RequestSentToProviderEvent requestSentToProviderEvent = RequestSentToProviderEvent.Create(this.AggregateId, sender,destination,message);

            this.ApplyAndPend(requestSentToProviderEvent);
        }

        /// <summary>
        /// Marks the message as failed.
        /// </summary>
        /// <param name="providerStatus">The provider status.</param>
        /// <param name="failedDateTime">The failed date time.</param>
        public void MarkMessageAsExpired(String providerStatus,
                                        DateTime failedDateTime)
        {
            this.CheckMessageCanBeSetToExpired();

            MessageExpiredEvent messageExpiredEvent = MessageExpiredEvent.Create(this.AggregateId, providerStatus, failedDateTime);

            this.ApplyAndPend(messageExpiredEvent);
        }

        /// <summary>
        /// Marks the message as failed.
        /// </summary>
        /// <param name="providerStatus">The provider status.</param>
        /// <param name="failedDateTime">The failed date time.</param>
        public void MarkMessageAsRejected(String providerStatus,
                                          DateTime failedDateTime)
        {
            this.CheckMessageCanBeSetToRejected();

            MessageRejectedEvent messageRejectedEvent = MessageRejectedEvent.Create(this.AggregateId, providerStatus, failedDateTime);

            this.ApplyAndPend(messageRejectedEvent);
        }

        /// <summary>
        /// Marks the message as delivered.
        /// </summary>
        /// <param name="providerStatus">The provider status.</param>
        /// <param name="failedDateTime">The failed date time.</param>
        public void MarkMessageAsDelivered(String providerStatus,
                                           DateTime failedDateTime)
        {
            this.CheckMessageCanBeSetToDelivered();

            MessageDeliveredEvent messageDeliveredEvent = MessageDeliveredEvent.Create(this.AggregateId, providerStatus, failedDateTime);

            this.ApplyAndPend(messageDeliveredEvent);
        }


        /// <summary>
        /// Marks the message as undeliverable.
        /// </summary>
        /// <param name="providerStatus">The provider status.</param>
        /// <param name="failedDateTime">The failed date time.</param>
        public void MarkMessageAsUndeliverable(String providerStatus,
                                               DateTime failedDateTime)
        {
            this.CheckMessageCanBeSetToUndeliverable();

            MessageUndeliveredEvent messageUndeliveredEvent = MessageUndeliveredEvent.Create(this.AggregateId, providerStatus, failedDateTime);

            this.ApplyAndPend(messageUndeliveredEvent);
        }

        /// <summary>
        /// Checks the message can be set to expired.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Message at status {this.MessageStatus} cannot be set to expired</exception>
        private void CheckMessageCanBeSetToExpired()
        {
            if (this.MessageStatus != MessageStatus.Sent)
            {
                throw new InvalidOperationException($"Message at status {this.MessageStatus} cannot be set to expired");
            }
        }

        /// <summary>
        /// Checks the message can be set to rejected.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Message at status {this.MessageStatus} cannot be set to rejected</exception>
        private void CheckMessageCanBeSetToRejected()
        {
            if (this.MessageStatus != MessageStatus.Sent)
            {
                throw new InvalidOperationException($"Message at status {this.MessageStatus} cannot be set to rejected");
            }
        }
        /// <summary>
        /// Checks the message can be set to delivered.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Message at status {this.MessageStatus} cannot be set to delivered</exception>
        private void CheckMessageCanBeSetToDelivered()
        {
            if (this.MessageStatus != MessageStatus.Sent)
            {
                throw new InvalidOperationException($"Message at status {this.MessageStatus} cannot be set to delivered");
            }
        }

        /// <summary>
        /// Checks the message can be set to undeliverable.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Message at status {this.MessageStatus} cannot be set to undeliverable</exception>
        private void CheckMessageCanBeSetToUndeliverable()
        {
            if (this.MessageStatus != MessageStatus.Sent)
            {
                throw new InvalidOperationException($"Message at status {this.MessageStatus} cannot be set to undeliverable");
            }
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        protected override Object GetMetadata()
        {
            return null;
        }

        /// <summary>
        /// Plays the event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        protected override void PlayEvent(DomainEvent domainEvent)
        {
            this.PlayEvent((dynamic)domainEvent);
        }

        /// <summary>
        /// Plays the event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        private void PlayEvent(RequestSentToProviderEvent domainEvent)
        {
            this.MessageStatus = MessageStatus.InProgress;
            this.Sender = domainEvent.Sender;
            this.Destination = domainEvent.Destination;
            this.Message = domainEvent.Message;
        }

        /// <summary>
        /// Plays the event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        private void PlayEvent(ResponseReceivedFromProviderEvent domainEvent)
        {
            this.ProviderReference = domainEvent.ProviderSMSReference;
            this.MessageStatus = MessageStatus.Sent;
        }

        /// <summary>
        /// Plays the event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        private void PlayEvent(MessageExpiredEvent domainEvent)
        {
            this.MessageStatus = MessageStatus.Expired;
        }

        /// <summary>
        /// Plays the event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        private void PlayEvent(MessageDeliveredEvent domainEvent)
        {
            this.MessageStatus = MessageStatus.Delivered;
        }

        /// <summary>
        /// Plays the event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        private void PlayEvent(MessageRejectedEvent domainEvent)
        {
            this.MessageStatus = MessageStatus.Rejected;
        }

        /// <summary>
        /// Plays the event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        private void PlayEvent(MessageUndeliveredEvent domainEvent)
        {
            this.MessageStatus = MessageStatus.Undeliverable;
        }
    }
}