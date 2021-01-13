﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BirdsiteLive.ActivityPub.Models;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Domain;
using BirdsiteLive.Pipeline.Processors.SubTasks;
using BirdsiteLive.Twitter.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BirdsiteLive.Pipeline.Tests.Processors.SubTasks
{
    [TestClass]
    public class SendTweetsToInboxTaskTests
    {
        [TestMethod]
        public async Task ExecuteAsync_SingleTweet_Test()
        {
            #region Stubs
            var tweetId = 10;
            var tweets = new List<ExtractedTweet>
            {
                new ExtractedTweet
                {
                    Id = tweetId,
                }
            };

            var noteId = "noteId";
            var note = new Note()
            {
                id = noteId
            };

            var twitterHandle = "Test";
            var twitterUserId = 7;
            var twitterUser = new SyncTwitterUser
            {
                Id = twitterUserId,
                Acct = twitterHandle
            };

            var host = "domain.ext";
            var inbox = "/user/inbox";
            var follower = new Follower
            {
                Id = 1,
                Host = host,
                InboxRoute = inbox,
                FollowingsSyncStatus = new Dictionary<int, long> { { twitterUserId, 9 } }
            };
            #endregion

            #region Mocks
            var activityPubService = new Mock<IActivityPubService>(MockBehavior.Strict);
            activityPubService
                .Setup(x => x.PostNewNoteActivity(
                    It.Is<Note>(y => y.id == noteId),
                    It.Is<string>(y => y == twitterHandle),
                    It.Is<string>(y => y == tweetId.ToString()),
                    It.Is<string>(y => y == host),
                    It.Is<string>(y => y == inbox)))
                .ReturnsAsync(HttpStatusCode.Accepted);

            var statusServiceMock = new Mock<IStatusService>(MockBehavior.Strict);
            statusServiceMock
                .Setup(x => x.GetStatus(
                It.Is<string>(y => y == twitterHandle),
                It.Is<ExtractedTweet>(y => y.Id == tweetId)))
                .Returns(note);

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            followersDalMock
                .Setup(x => x.UpdateFollowerAsync(
                    It.Is<Follower>(y => y.Id == follower.Id && y.FollowingsSyncStatus[twitterUserId] == tweetId)))
                .Returns(Task.CompletedTask);

            #endregion

            var task = new SendTweetsToInboxTask(activityPubService.Object, statusServiceMock.Object, followersDalMock.Object);
            await task.ExecuteAsync(tweets.ToArray(), follower, twitterUser);

            #region Validations
            activityPubService.VerifyAll();
            statusServiceMock.VerifyAll();
            followersDalMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        public async Task ExecuteAsync_MultipleTweets_Test()
        {
            #region Stubs
            var tweetId1 = 10;
            var tweetId2 = 11;
            var tweetId3 = 12;
            var tweets = new List<ExtractedTweet>();
            foreach (var tweetId in new[] { tweetId1, tweetId2, tweetId3 })
            {
                tweets.Add(new ExtractedTweet
                {
                    Id = tweetId
                });
            }

            var twitterHandle = "Test";
            var twitterUserId = 7;
            var twitterUser = new SyncTwitterUser
            {
                Id = twitterUserId,
                Acct = twitterHandle
            };

            var host = "domain.ext";
            var inbox = "/user/inbox";
            var follower = new Follower
            {
                Id = 1,
                Host = host,
                InboxRoute = inbox,
                FollowingsSyncStatus = new Dictionary<int, long> { { twitterUserId, 10 } }
            };
            #endregion

            #region Mocks
            var activityPubService = new Mock<IActivityPubService>(MockBehavior.Strict);
            foreach (var tweetId in new[] { tweetId2, tweetId3 })
            {
                activityPubService
                    .Setup(x => x.PostNewNoteActivity(
                        It.Is<Note>(y => y.id == tweetId.ToString()),
                        It.Is<string>(y => y == twitterHandle),
                        It.Is<string>(y => y == tweetId.ToString()),
                        It.Is<string>(y => y == host),
                        It.Is<string>(y => y == inbox)))
                    .ReturnsAsync(HttpStatusCode.Accepted);
            }

            var statusServiceMock = new Mock<IStatusService>(MockBehavior.Strict);
            foreach (var tweetId in new[] { tweetId2, tweetId3 })
            {
                statusServiceMock
                    .Setup(x => x.GetStatus(
                        It.Is<string>(y => y == twitterHandle),
                        It.Is<ExtractedTweet>(y => y.Id == tweetId)))
                    .Returns(new Note { id = tweetId.ToString() });
            }

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            followersDalMock
                .Setup(x => x.UpdateFollowerAsync(
                    It.Is<Follower>(y => y.Id == follower.Id && y.FollowingsSyncStatus[twitterUserId] == tweetId3)))
                .Returns(Task.CompletedTask);

            #endregion

            var task = new SendTweetsToInboxTask(activityPubService.Object, statusServiceMock.Object, followersDalMock.Object);
            await task.ExecuteAsync(tweets.ToArray(), follower, twitterUser);

            #region Validations
            activityPubService.VerifyAll();
            statusServiceMock.VerifyAll();
            followersDalMock.VerifyAll();
            #endregion
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task ExecuteAsync_MultipleTweets_Error_Test()
        {
            #region Stubs
            var tweetId1 = 10;
            var tweetId2 = 11;
            var tweetId3 = 12;
            var tweets = new List<ExtractedTweet>();
            foreach (var tweetId in new[] { tweetId1, tweetId2, tweetId3 })
            {
                tweets.Add(new ExtractedTweet
                {
                    Id = tweetId
                });
            }

            var twitterHandle = "Test";
            var twitterUserId = 7;
            var twitterUser = new SyncTwitterUser
            {
                Id = twitterUserId,
                Acct = twitterHandle
            };

            var host = "domain.ext";
            var inbox = "/user/inbox";
            var follower = new Follower
            {
                Id = 1,
                Host = host,
                InboxRoute = inbox,
                FollowingsSyncStatus = new Dictionary<int, long> { { twitterUserId, 10 } }
            };
            #endregion

            #region Mocks
            var activityPubService = new Mock<IActivityPubService>(MockBehavior.Strict);

            activityPubService
                .Setup(x => x.PostNewNoteActivity(
                    It.Is<Note>(y => y.id == tweetId2.ToString()),
                    It.Is<string>(y => y == twitterHandle),
                    It.Is<string>(y => y == tweetId2.ToString()),
                    It.Is<string>(y => y == host),
                    It.Is<string>(y => y == inbox)))
                .ReturnsAsync(HttpStatusCode.Accepted);

            activityPubService
                .Setup(x => x.PostNewNoteActivity(
                    It.Is<Note>(y => y.id == tweetId3.ToString()),
                    It.Is<string>(y => y == twitterHandle),
                    It.Is<string>(y => y == tweetId3.ToString()),
                    It.Is<string>(y => y == host),
                    It.Is<string>(y => y == inbox)))
                .ReturnsAsync(HttpStatusCode.InternalServerError);

            var statusServiceMock = new Mock<IStatusService>(MockBehavior.Strict);
            foreach (var tweetId in new[] { tweetId2, tweetId3 })
            {
                statusServiceMock
                    .Setup(x => x.GetStatus(
                        It.Is<string>(y => y == twitterHandle),
                        It.Is<ExtractedTweet>(y => y.Id == tweetId)))
                    .Returns(new Note { id = tweetId.ToString() });
            }

            var followersDalMock = new Mock<IFollowersDal>(MockBehavior.Strict);
            followersDalMock
                .Setup(x => x.UpdateFollowerAsync(
                    It.Is<Follower>(y => y.Id == follower.Id && y.FollowingsSyncStatus[twitterUserId] == tweetId2)))
                .Returns(Task.CompletedTask);

            #endregion

            var task = new SendTweetsToInboxTask(activityPubService.Object, statusServiceMock.Object, followersDalMock.Object);

            try
            {
                await task.ExecuteAsync(tweets.ToArray(), follower, twitterUser);
            }
            finally
            {
                #region Validations
                activityPubService.VerifyAll();
                statusServiceMock.VerifyAll();
                followersDalMock.VerifyAll();
                #endregion
            }
        }
    }
}