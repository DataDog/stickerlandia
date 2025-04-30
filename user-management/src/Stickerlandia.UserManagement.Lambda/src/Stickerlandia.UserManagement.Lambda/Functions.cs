using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Stickerlandia.UserManagement.Lambda;

/// <summary>
/// A collection of sample Lambda functions that provide a REST api for doing simple math calculations. 
/// </summary>
public class Functions
{
    [LambdaFunction()]
    [HttpApi(LambdaHttpMethod.Get, "/")]
    public string Default()
    {
        var docs = @"Lambda Calculator Home:
You can make the following requests to invoke other Lambda functions perform calculator operations:
/add/{x}/{y}
/subtract/{x}/{y}
/multiply/{x}/{y}
/divide/{x}/{y}
";
        return docs;
    }
}