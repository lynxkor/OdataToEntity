﻿using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using OdataToEntity.Parsers;
using OdataToEntity.Parsers.UriCompare;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace OdataToEntity.Test
{
    public sealed class QueryComparerTest
    {
        private struct RequestMethodName
        {
            public readonly String[] EqualMethodNames;
            public readonly String MethodName;
            public readonly String Request;

            public RequestMethodName(String request, String methodName, String[] equalMethodNames)
            {
                Request = request;
                MethodName = methodName;
                EqualMethodNames = equalMethodNames;
            }
        }

        private sealed class QueryComparerFixture : DbFixtureInitDb
        {
            private readonly List<RequestMethodName> _requests;

            public QueryComparerFixture()
            {
                _requests = new List<RequestMethodName>();
            }

            public override Task Execute<T, TResult>(QueryParameters<T, TResult> parameters)
            {
                _requests.Add(new RequestMethodName(parameters.RequestUri, null, parameters.EqualMethodNames));
                return Task.CompletedTask;
            }
            public override Task Execute<T, TResult>(QueryParametersScalar<T, TResult> parameters)
            {
                _requests.Add(new RequestMethodName(parameters.RequestUri, null, parameters.EqualMethodNames));
                return Task.CompletedTask;

            }
            public override void Initalize()
            {
            }

            public IReadOnlyList<RequestMethodName> Requests => _requests;
        }

        private static async Task<RequestMethodName[]> GetRequestMethodNames(QueryComparerFixture fixture)
        {
            var methodNames = new List<String>();
            var selectTest = new SelectTest(fixture);
            foreach (MethodInfo methodInfo in selectTest.GetType().GetMethods().Where(m => m.GetCustomAttributes(typeof(FactAttribute), false).Count() == 1))
            {
                methodNames.Add(methodInfo.Name);
                var testMethod = (Func<SelectTest, Task>)methodInfo.CreateDelegate(typeof(Func<SelectTest, Task>));
                await testMethod(selectTest);
            }

            var requestMethodNames = new RequestMethodName[methodNames.Count];
            for (int i = 0; i < methodNames.Count; i++)
                requestMethodNames[i] = new RequestMethodName(fixture.Requests[i].Request, methodNames[i], fixture.Requests[i].EqualMethodNames);
            return requestMethodNames;
        }
        //[Fact]
        public async Task Test()
        {
            var fixture = new QueryComparerFixture();
            RequestMethodName[] requestMethodNames = await GetRequestMethodNames(fixture);

            var parser = new OeGetParser(new Uri("http://dummy/"), fixture.OeDataAdapter, fixture.EdmModel);
            for (int i = 0; i < requestMethodNames.Length; i++)
            {
                OeParseUriContext parseUriContext1 = parser.ParseUri(new Uri(parser.BaseUri + requestMethodNames[i].Request));
                OeParseUriContext parseUriContext2 = parser.ParseUri(new Uri(parser.BaseUri + requestMethodNames[i].Request));

                var constantNodeNames = new FakeReadOnlyDictionary<ConstantNode, String>();
                bool result = new OeODataUriComparer(constantNodeNames).Compare(parseUriContext1, parseUriContext2);
                Assert.True(result);

                for (int j = i + 1; j < requestMethodNames.Length; j++)
                {
                    parseUriContext1 = parser.ParseUri(new Uri(parser.BaseUri + requestMethodNames[i].Request));
                    parseUriContext2 = parser.ParseUri(new Uri(parser.BaseUri + requestMethodNames[j].Request));
                    constantNodeNames = new FakeReadOnlyDictionary<ConstantNode, String>();
                    result = new OeODataUriComparer(constantNodeNames).Compare(parseUriContext1, parseUriContext2);

                    if (result && requestMethodNames[j].EqualMethodNames != null &&
                        requestMethodNames[j].EqualMethodNames.Contains(requestMethodNames[i].MethodName))
                        Assert.True(result);
                    else
                        Assert.False(result);
                }
            }
        }
    }
}