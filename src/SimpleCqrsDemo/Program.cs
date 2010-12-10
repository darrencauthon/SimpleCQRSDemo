using System;
using System.Collections.Generic;
using System.Linq;
using SimpleCqrs;
using SimpleCqrs.Commanding;
using SimpleCqrs.Domain;
using SimpleCqrs.Eventing;
using SimpleCqrs.Unity;

namespace SimpleCqrsDemo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var runtime = new SampleRuntime();

            // start up the runtime
            // SimpleCQRS will scan assemblies, find all the pieces, and wire them up for you
            runtime.Start();

            // use this "table" as a fake database table, load a single instance of it in the container
            // this has nothing to do with SimpleCQRS, just demoing possible results without a database
            var accountReportTable = new AccountReportTable();
            runtime.ServiceLocator.Register(accountReportTable);

            // fire the command
            var commandBus = runtime.ServiceLocator.Resolve<ICommandBus>();
            commandBus.Send(new CreateAccountCommand { FirstName = "Darren", LastName = "Cauthon" });

            // check the results in the table?
            // or try writing your own denormalizer?

            runtime.Shutdown();
        }
    }

    public class AccountReportDenormalizer : IHandleDomainEvents<AccountCreatedEvent>,
                                             IHandleDomainEvents<AccountNameSetEvent>
    {
        private readonly AccountReportTable accountReportTable;

        public AccountReportDenormalizer(AccountReportTable accountReportTable)
        {
            this.accountReportTable = accountReportTable;
        }

        public void Handle(AccountCreatedEvent domainEvent)
        {
            accountReportTable.Add(new AccountReportRow {Id = domainEvent.AggregateRootId});
        }

        public void Handle(AccountNameSetEvent domainEvent)
        {
            accountReportTable.Single(x => x.Id == domainEvent.AggregateRootId)
                .Name = domainEvent.FirstName + " " + domainEvent.LastName;
        }
    }

    public class CreateAccountCommandHandler : CommandHandler<CreateAccountCommand>
    {
        private readonly IDomainRepository domainRepository;

        public CreateAccountCommandHandler(IDomainRepository domainRepository)
        {
            this.domainRepository = domainRepository;
        }

        public override void Handle(CreateAccountCommand command)
        {
            var account = new Account(Guid.NewGuid());
            account.SetName(command.FirstName, command.LastName);
            domainRepository.Save(account);
        }
    }

    public class Account : AggregateRoot
    {
        public Account(Guid id)
        {
            Apply(new AccountCreatedEvent {AggregateRootId = id});
        }

        public void OnAccountCreated(AccountCreatedEvent accountCreatedEvent)
        {
            Id = accountCreatedEvent.AggregateRootId;
        }

        public void SetName(string firstName, string lastName)
        {
            Apply(new AccountNameSetEvent {FirstName = firstName, LastName = lastName});
        }
    }

    public class AccountNameSetEvent : DomainEvent
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class AccountCreatedEvent : DomainEvent
    {
    }

    public class CreateAccountCommand : ICommand
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class SampleRuntime : SimpleCqrsRuntime<UnityServiceLocator>
    {
    }

    public class AccountReportTable : List<AccountReportRow>
    {
    }

    public class AccountReportRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

}