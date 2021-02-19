
using System;
using System.Diagnostics;
using System.Threading;
using Azure.Core;
using Azure.Identity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void AccessTokenTest()
        {
            var provider = My.Services.GetService<IConfidentialClientCredentialProvider>();

            // First call.
            var timer = Stopwatch.StartNew();
            _ = provider.GetTokenCredential("aad");
            timer.Stop();
            Console.WriteLine($"First call: {timer.Elapsed.TotalMilliseconds:#,0.000} milliSec"); ;

            // Subsequent calls.
            int loopCount = 1000000;
            timer.Restart();
            for (int i = 0; i < loopCount; i++) _ = provider.GetTokenCredential("aad");
            timer.Stop();

            var elapsed = timer.Elapsed.TotalMilliseconds;
            var averageMicroSec = elapsed / (double)loopCount * 1000.0;

            Console.WriteLine($"{loopCount:#,0} iterations Avg: {averageMicroSec:#,0.000} µSec");
        }

        [TestMethod]
        public void AccessTokenIsCached()
        {
            var provider = My.Services.GetService<IConfidentialClientCredentialProvider>();

            var scopes = new string[] { "https://storage.azure.com/.default" };
            var ctx = new TokenRequestContext(scopes);

            var token1 = provider
                .GetTokenCredential("aad")
                .GetToken(ctx, CancellationToken.None);

            var token2 = provider
                .GetTokenCredential("aad")
                .GetToken(ctx, CancellationToken.None);

            Console.WriteLine($"Token expires in: {token1.ExpiresOn - DateTimeOffset.UtcNow}");
            Console.WriteLine();

            var lastDot = token1.Token.LastIndexOf('.');
            var tokenWihoutSignature = token1.Token.Substring(0, lastDot);
            Console.WriteLine(tokenWihoutSignature);

            Assert.AreEqual(token1.Token, token2.Token);
            Assert.ReferenceEquals(token1.Token, token2.Token);
        }
    }
}
