# EventStoreDB .NET TCP Client

EventStoreDB is the open-source, functional database with Complex Event Processing in JavaScript.

This is the repository for the legacy .NET TCP client for EventStoreDB version 5+ and uses TCP as the communication protocol.  
If you're looking for the new recommended .NET gRPC client, check the [EventStore-Client-Dotnet](https://github.com/EventStore/EventStore-Client-Dotnet) repo.

## Support

Information on support and commercial tools such as LDAP authentication can be found here: [Event Store Support](https://eventstore.com/support/).

## CI Status

[![Build](https://github.com/EventStore/EventStoreDB-Client-Dotnet-Legacy/actions/workflows/ci.yml/badge.svg)](https://github.com/EventStore/EventStoreDB-Client-Dotnet-Legacy/actions/workflows/ci.yml)

## Running the Tests

The tests start up a secure Event Store node in Docker, so you need to create certificates for this node before running the tests.  
To do this locally, download the [es-gencert-cli tool](https://github.com/EventStore/es-gencert-cli) to the root directory.

Then create the certificates for the tests in the `certs` directory:

```
.\es-gencert-cli create-ca --out .\certs\ca

.\es-gencert-cli create-node --out .\certs\node --ca-certificate .\certs\ca\ca.crt --ca-key .\certs\ca\ca.key --ip-addresses=127.0.0.1 --dns-names=localhost
```

## Documentation

Documentation for EventStoreDB can be found here: [Event Store Docs](https://eventstore.com/docs/).

## Community

We have a community discussion space at [Event Store Discuss](https://discuss.eventstore.com/). If you prefer slack, there is also an #eventstore channel in the [DDD-CQRS-ES](https://j.mp/ddd-es-cqrs) slack community.

## Contributing

Development is done on the `master` branch.  
We attempt to do our best to ensure that the history remains clean and to do so, we generally ask contributors to squash their commits into a set or single logical commit.