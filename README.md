# MyInfluxDbClient
Simple async client for interacting with [InfluxDB](http://influxdb.com).

It's under continous developement, but can already be used to perform various database operations and write points, that you then e.g. can use in [Grafana dashboards](http://grafana.org/).

## Roadmap
Track all features etc. via the [Issues](https://github.com/danielwertheim/myinfluxdbclient/issues) & [Milestones](https://github.com/danielwertheim/myinfluxdbclient/milestones).

The version will be `pre v1.0.0` until the milestones: [Gecko](https://github.com/danielwertheim/myinfluxdbclient/milestones/Gecko) and [Schema exploration](https://github.com/danielwertheim/myinfluxdbclient/milestones/Schema%20exploration); are done. Potentially also [Digger](https://github.com/danielwertheim/myinfluxdbclient/milestones/Digger)

## Getting setup
All operations are currently located on `InfluxDbClient` which extends the interface `IInfluxDbClient`. Internally it makes use of [Requester](https://github.com/danielwertheim/requester) to perform all HTTP-requests.

Install [the NuGet package](https://www.nuget.org/packages/myinfluxdbclient):
```
install-package MyInfluxDbClient
```

then create a client:

```csharp
using (var client = new InfluxDbClient("http://192.168.1.176:8086"))
{
	//consume client
}
```

you are now ready to start consuming it. See below for available operations.

## Exceptions
Simple argument validation will throw either `ArgumentException` or a derivate of it. This could be thrown if you e.g. pass `null` for an operation that requires a `databaseName` to be passed.

If, for some reason the call against InfluxDB fails, an `InfluxDbClientException` will be thrown. For more info of why it failed, you can inspect the members:

```csharp
public class InfluxDbClientException : Exception
{
	public HttpMethod HttpMethod { get; }
	public HttpStatusCode HttpStatus { get; }
	public Uri Uri { get; }
	public string Reason { get; }
	public string Content { get; }
	...
	...
}
```

## Database operations
Currently, the ops throws if InfluxDB returns failures. There will be additional, complementary operations e.g. `EnsureDatabaseExistsAsync(dbName)` that will not throw if a database already exists.

- `client.GetDatabaseNamesAsync():Task<string[]>` - returns an array of database name. **Note!** All databases are returned, even system databases.
- `client.DatabaseExistsAsync(dbName):Task<bool>` -  checks if a database exists. Note. Makes use of `GetDatabaseNamesAsync` to compare.
- `client.CreateDatabaseAsync(dbName):Task` - create a database. **Note!** Throws if the database already exists.
- `client.DropDatabaseAsync(dbName):Task` - drop an existing database. **Note!** Throws if the database does not exist.

## Write points
The `MyInfluxDbClient` makes use of the [Line Protocol](https://influxdb.com/docs/v0.9/write_protocols/line.html) when writing points.

You write points by calling `client.WriteAsync(points)`. Do this by:

1. Create an `InfluxPoints` instance
2. Add one or more `InfluxPoint` instances to the collection
3. Pass the `InfluxPoints` to `client.WriteAsync` and specify the database to write to.

```csharp
var points = new InfluxPoints()
	.Add(new InfluxPoint("ticketsold")
		.AddTag("seller", "1")
		.AddTag("cur", "SEK")
		.AddField("amt", 150.0)
		.AddField("fee", 50.0)) //TimeStamp is assigned in Influx
	.Add(new InfluxPoint("ticketsold")
		.AddTag("seller", "1")
		.AddTag("cur", "USD")
		.AddField("amt", 10.0)
		.AddField("fee", 2.5).AddTimeStamp())
	.Add(new InfluxPoint("ticketsold")
		.AddTag("seller", "2")
		.AddTag("cur", "USD")
		.AddField("amt", 10.0)
		.AddField("fee", 2.5)
		.AddTimeStamp(DateTime.Now)) //Conv. to nanoseconds since Epoch (UTC)
	.Add(new InfluxPoint("ticketsold")
		.AddTag("seller", "2")
		.AddTag("cur", "EUR")
		.AddField("amt", 51.8)
		.AddField("fee", 5.0)
		.AddTimeStamp(TimeStampResolutions.Minutes)) //Just keep timestamps to minutes. You can also pass a DateTime.

await client.WriteAsync("mydb", points);
```

## WriteOptions
There are some `WriteOptions` that you can configure either per-client or per call to `WriteAsync`.

**Note** These are just formatted according to [documentation](https://influxdb.com/docs/v0.9/write_protocols/write_syntax.html#http).

### WriteOptions per client (optional)
```csharp
//Configure write options on client (if you want)
client.DefaultWriteOptions
	.SetRetentionPolicy("test")
	.SetTimeStampPrecision(TimeStampPrecision.Minutes)
	.SetConsistency(Consistency.Quorum);
```

**Tip** If you want to reuse on all clients, then create a factory for creating your clients (or configure in your IoC container like Ninject).

### WriteOptions per WriteAsync call (optional)

```csharp
//Specific options can be passed if you want
var writeOptions = new WriteOptions()
	.SetConsistency(Consistency.Any);

await client.WriteAsync("mydb", points, writeOptions);
```
