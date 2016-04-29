﻿// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.

using System;
using System.Collections.Generic;
using Org.Apache.REEF.Driver.Context;
using Org.Apache.REEF.Driver.Evaluator;
using Org.Apache.REEF.IMRU.OnREEF.Driver;
using Xunit;

namespace Org.Apache.REEF.IMRU.Tests
{
    /// <summary>
    /// Test methods in ActiveContextManager
    /// </summary>
    public class TestActiveContextManager
    {
        private const string EvaluatorIdPrefix = "EvaluatorId";
        private const string ContextIdPrefix = "ContextId";

        /// <summary>
        /// Test add, Remove, RemovedFailedContextInFailedEvaluator, NumberOfMissingContexts and NumberOfActiveContext
        /// in ActiveContexManager
        /// </summary>
        [Fact]
        public void TestActiveContexManager()
        {
            var activeContextManager = new ActiveContextManager(5);
            for (int i = 0; i < 5; i++)
            {
                var c = new MyActiveContext(ContextIdPrefix + i);
                activeContextManager.Add(c);
            }
            Assert.True(activeContextManager.AllContextsReceived);
            Assert.Equal(5, activeContextManager.NumberOfActiveContexts);

            IList<string> contextIds = new List<string>();
            contextIds.Add(ContextIdPrefix + "3");
            var e = new MyFailedEvaluator(contextIds);
            activeContextManager.RemovedFailedContextInFailedEvaluator(e);
            Assert.Equal(1, activeContextManager.NumberOfMissingContexts);
            activeContextManager.Remove(ContextIdPrefix + "4");
            Assert.Equal(3, activeContextManager.NumberOfActiveContexts);
        }

        /// <summary>
        /// Test invalid Add and Remove
        /// </summary>
        public void TestInvalidAddRemoveCases()
        {
            var activeContextManager = new ActiveContextManager(3);
            activeContextManager.Add(new MyActiveContext(ContextIdPrefix + "1"));

            Action add = () => activeContextManager.Add(new MyActiveContext(ContextIdPrefix + "1"));
            Assert.Throws<IMRUSystemException>(add);

            Action remove = () => activeContextManager.Remove(ContextIdPrefix + "2");
            Assert.Throws<IMRUSystemException>(remove);

            activeContextManager.Add(new MyActiveContext(ContextIdPrefix + "2"));
            activeContextManager.Add(new MyActiveContext(ContextIdPrefix + "3"));

            add = () => activeContextManager.Add(new MyActiveContext(ContextIdPrefix + "4"));
            Assert.Throws<IMRUSystemException>(add);
        }

        /// <summary>
        /// Test remove a failed evaluator which has two contexts associated.
        /// In current IMRU driver, assume there is only one context associated to the IFailedEvalutor
        /// </summary>
        [Fact]
        public void TestRemoveFailedEvaluatorWithTwoContexts()
        {
            var activeContextManager = new ActiveContextManager(5);
            for (int i = 0; i < 5; i++)
            {
                var c = new MyActiveContext(ContextIdPrefix + i);
                activeContextManager.Add(c);
            }
            IList<string> contextIds = new List<string>();
            contextIds.Add(ContextIdPrefix + "3");
            contextIds.Add(ContextIdPrefix + "4");
            var e = new MyFailedEvaluator(contextIds);
            Action remove = () => activeContextManager.RemovedFailedContextInFailedEvaluator(e);
            Assert.Throws<IMRUSystemException>(remove);
        }

        /// <summary>
        /// Test remove a failed evaluator which has a context but it doesn't exist.
        /// </summary>
        [Fact]
        public void TestRemoveFailedEvaluatorWithNoExistsContexts()
        {
            var activeContextManager = new ActiveContextManager(5);
            for (int i = 0; i < 5; i++)
            {
                var c = new MyActiveContext(ContextIdPrefix + i);
                activeContextManager.Add(c);
            }

            IList<string> contextIds = new List<string>();
            contextIds.Add(ContextIdPrefix + "5");
            var e = new MyFailedEvaluator(contextIds);
            Action remove = () => activeContextManager.RemovedFailedContextInFailedEvaluator(e);
            Assert.Throws<IMRUSystemException>(remove);
        }

        /// <summary>
        /// Test remove a failed evaluator which has no context associated.
        /// The scenario may happen when an evaluator failed but context has not created yet. 
        /// </summary>
        [Fact]
        public void TestRemoveFailedEvaluatorWithNoContext()
        {
            var activeContextManager = new ActiveContextManager(5);
            for (int i = 0; i < 5; i++)
            {
                var c = new MyActiveContext(ContextIdPrefix + i);
                activeContextManager.Add(c);
            }

            var e = new MyFailedEvaluator(null);
            activeContextManager.RemovedFailedContextInFailedEvaluator(e);
            Assert.Equal(5, activeContextManager.NumberOfActiveContexts);
        }

        /// <summary>
        /// An implementation of IFailedEvaluator for testing
        /// </summary>
        private class MyFailedEvaluator : IFailedEvaluator
        {
            private readonly IList<string> _contextIds;

            /// <summary>
            /// Constructor of MyFailedEvaluator
            /// The contextIds is used in the associated IFailedContext 
            /// </summary>
            /// <param name="contextIds"></param>
            internal MyFailedEvaluator(IList<string> contextIds)
            {
                _contextIds = contextIds;
            }

            public EvaluatorException EvaluatorException
            {
                get { throw new NotImplementedException(); }
            }

            /// <summary>
            /// Have one failed context in the failed Evaluator
            /// </summary>
            public IList<IFailedContext> FailedContexts
            {
                get
                {
                    if (_contextIds == null)
                    {
                        return null;
                    }

                    IList<IFailedContext> contexts = new List<IFailedContext>();
                    foreach (var cid in _contextIds)
                    {
                        contexts.Add(new MyFailedContext(cid));
                    }
                    return contexts;
                }
            }

            public Utilities.Optional<Driver.Task.IFailedTask> FailedTask
            {
                get { throw new NotImplementedException(); }
            }

            /// <summary>
            /// Returns Evaluator id
            /// </summary>
            public string Id
            {
                get
                {
                    if (_contextIds != null && _contextIds.Count == 1)
                    {
                        return EvaluatorIdPrefix + _contextIds[0];
                    }
                    return EvaluatorIdPrefix + "no";
                }
            }
        }

        private class MyFailedContext : IFailedContext
        {
            private readonly string _id;

            internal MyFailedContext(string id)
            {
                _id = id;
            }

            public Utilities.Optional<IActiveContext> ParentContext
            {
                get { throw new NotImplementedException(); }
            }

            public string EvaluatorId
            {
                get { return EvaluatorIdPrefix + _id; }
            }

            public Utilities.Optional<string> ParentId
            {
                get { throw new NotImplementedException(); }
            }

            public IEvaluatorDescriptor EvaluatorDescriptor
            {
                get { throw new NotImplementedException(); }
            }

            public string Id
            {
                get { return _id; }
            }
        }

        /// <summary>
        /// An implementation of IActiveContext for testing
        /// </summary>
        private sealed class MyActiveContext : IActiveContext
        {
            private readonly string _id;

            /// <summary>
            /// Constructor which gets id of MyActiveContext
            /// </summary>
            /// <param name="id"></param>
            internal MyActiveContext(string id)
            {
                _id = id;
            }

            public void SendMessage(byte[] message)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Returns Evaluator Id of the MyActiveContext
            /// </summary>
            public string EvaluatorId
            {
                get { return EvaluatorIdPrefix + _id; }
            }

            public Utilities.Optional<string> ParentId
            {
                get { throw new NotImplementedException(); }
            }

            public IEvaluatorDescriptor EvaluatorDescriptor
            {
                get { throw new NotImplementedException(); }
            }

            /// <summary>
            /// Returns Id of the Active Context
            /// </summary>
            public string Id
            {
                get { return _id; }
            }

            public void SubmitTask(Tang.Interface.IConfiguration taskConf)
            {
                throw new NotImplementedException();
            }

            public void SubmitContext(Tang.Interface.IConfiguration contextConfiguration)
            {
                throw new NotImplementedException();
            }

            public void SubmitContextAndService(Tang.Interface.IConfiguration contextConfiguration, Tang.Interface.IConfiguration serviceConfiguration)
            {
                throw new NotImplementedException();
            }
        }
    }
}