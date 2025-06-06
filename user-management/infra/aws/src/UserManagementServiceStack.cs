using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace UserManagementService;

public class UserManagementServiceStack : Stack
{
    internal UserManagementServiceStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        var serviceName = "UserManagementService";
        var env = System.Environment.GetEnvironmentVariable("ENV") ?? "dev";
        var version = System.Environment.GetEnvironmentVariable("VERSION") ?? "latest";
        var ddSite = System.Environment.GetEnvironmentVariable("DD_SITE") ?? "datadoghq.com";

        var secret = new Secret(this, "DDApiKeySecret", new SecretProps
        {
            SecretName = $"/{env}/{serviceName}/dd-api-key",
            SecretStringValue = new SecretValue(System.Environment.GetEnvironmentVariable("DD_API_KEY") ??
                                                throw new Exception("DD_API_KEY environment variable is not set"))
        });

        var team = "users";
        var domain = "users";
        var sharedProps = new SharedProps(serviceName, env, version, team, domain, secret, ddSite);

        var userRegisteredTopic = new Topic(this, "UserRegisteredTopic", new TopicProps
        {
            TopicName = $"{sharedProps.ServiceName}-user-registered-{sharedProps.Env}"
        });
        var stickerClaimedDLQ = new Queue(this, "StickerClaimedDLQ", new QueueProps
        {
            QueueName = $"{sharedProps.ServiceName}-sticker-claimed-dlq-{sharedProps.Env}"
        });
        var stickerClaimedQueue = new Queue(this, "StickerClaimedQueue", new QueueProps
        {
            QueueName = $"{sharedProps.ServiceName}-sticker-claimed-{sharedProps.Env}",
            DeadLetterQueue = new DeadLetterQueue
            {
                MaxReceiveCount = 3,
                Queue = stickerClaimedDLQ
            }
        });

        var defaultEnvironmentVariables = new Dictionary<string, string>
        {
            {
                "ConnectionStrings__database",
                "Host=ep-weathered-wave-ab469hjs-pooler.eu-west-2.aws.neon.tech;Port=5432;Username=stickerlandia-users_owner;Password=npg_buwe2PoK1NgV;Database=stickerlandia-users"
            },
            { "Aws__UserRegisteredTopicArn", userRegisteredTopic.TopicArn },
            { "Aws__StickerClaimedQueueUrl", stickerClaimedQueue.QueueUrl },
            { "Aws__StickerClaimedDLQUrl", stickerClaimedDLQ.QueueUrl },
            { "DRIVING", "AWS" },
            { "DRIVEN", "AWS" }
        };

        var dbMigrationFunction = new InstrumentedFunction(
            this,
            "UserManagementDbMigrationFunction",
            new FunctionProps(
                sharedProps,
                "UserManagementDbMigration",
                "../../src/Stickerlandia.UserManagement.Lambda/",
                "Stickerlandia.UserManagement.Lambda::Stickerlandia.UserManagement.Lambda.MigrationFunction_Migrate_Generated::Migrate",
                defaultEnvironmentVariables
            )
        );

        var stickerClaimedFunction = new InstrumentedFunction(
            this,
            "UserManagementStickerClaimedFunction",
            new FunctionProps(
                sharedProps,
                "UserManagementStickerClaimedFunction",
                "../../src/Stickerlandia.UserManagement.Lambda/",
                "Stickerlandia.UserManagement.Lambda::Stickerlandia.UserManagement.Lambda.Sqs_StickerClaimed_Generated::StickerClaimed",
                defaultEnvironmentVariables
            )
        );
        stickerClaimedFunction.Function.AddEventSource(new SqsEventSource(stickerClaimedQueue,
            new SqsEventSourceProps
            {
                ReportBatchItemFailures = true
            }));

        var outboxWorker = new InstrumentedFunction(
            this,
            "UserManagementOutboxWorker",
            new FunctionProps(
                sharedProps,
                "UserManagementOutboxWorker",
                "../../src/Stickerlandia.UserManagement.Lambda/",
                "Stickerlandia.UserManagement.Lambda::Stickerlandia.UserManagement.Lambda.OutboxFunctions_Worker_Generated::Worker",
                defaultEnvironmentVariables
            )
        );
        // To run the outbox worker once per minute
        var scheduleRule = new Rule(this, "OutboxWorkerSchedule", new RuleProps
        {
            Schedule = Schedule.Rate(Duration.Minutes(1)),
            Targets = new IRuleTarget[]
            {
                new LambdaFunction(outboxWorker.Function, new LambdaFunctionProps
                {
                    // Optional: Retry policy
                    RetryAttempts = 2
                })
            }
        });
        userRegisteredTopic.GrantPublish(outboxWorker.Function);

        var apiFunction = new InstrumentedFunction(
            this,
            "UserManagementApiFunction",
            new FunctionProps(
                sharedProps,
                "UserManagementApi",
                "../../src/Stickerlandia.UserManagement.Api/",
                "Stickerlandia.UserManagement.Api",
                defaultEnvironmentVariables
            )
        );

        var api = new RestApi(this, "UserManagementApi", new RestApiProps
        {
            RestApiName = $"${serviceName}-api-${env}"
        });
        var proxyResource = api.Root.AddResource("{proxy+}");
        proxyResource.AddMethod("ANY", new LambdaIntegration(apiFunction.Function));
    }
}