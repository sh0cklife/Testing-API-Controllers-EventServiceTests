using Homies.Data;
using Homies.Data.Models;
using Homies.Models.Event;
using Homies.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace Homies.Tests
{
    [TestFixture]
    internal class EventServiceTests
    {
        private HomiesDbContext _dbContext;
        private EventService _eventService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<HomiesDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use unique database name to avoid conflicts
                .Options;
            _dbContext = new HomiesDbContext(options);

            _eventService = new EventService(_dbContext);
        }

        [Test]
        public async Task AddEventAsync_ShouldAddEvent_WhenValidEventModelAndUserId()
        {
            // Step 1: Arrange - Set up the initial conditions for the test
            // Create a new event model with test data
            var eventModel = new EventFormModel
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2)
            };
            // Define a user ID for testing purposes
            string userId = "testUserId";

            // Step 2: Act - Perform the action being tested
            // Call the service method to add the event
            await _eventService.AddEventAsync(eventModel, userId);

            // Step 3: Assert - Verify the outcome of the action
            // Retrieve the added event from the database
            var eventInTheDatabase = await _dbContext.Events.FirstOrDefaultAsync(x => x.Name == eventModel.Name && x.OrganiserId == userId);

            // Assert that the added event is not null, indicating it was successfully added
            Assert.NotNull(eventInTheDatabase);
            // Assert that the description of the added event matches the description provided in the event model
            Assert.That(eventInTheDatabase.End, Is.EqualTo(eventModel.End));
            Assert.That(eventInTheDatabase.Description, Is.EqualTo(eventModel.Description));
            Assert.That(eventInTheDatabase.Start, Is.EqualTo(eventModel.Start));
            Assert.That(eventInTheDatabase.Name, Is.EqualTo(eventModel.Name));
            
           
            
        }


        [Test]
        public async Task GetAllEventsAsync_ShouldReturnAllEvents()
        {
            // Step 1: Arrange - Set up the initial conditions for the test
            // Create two event models with test data
            var eventModelOne = new EventFormModel
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2)
            };
            // Define a user ID for testing purposes
            string userIdOne = "testUserIdOne";

            var eventModelTwo = new EventFormModel
            {
                Name = "Test Event Two",
                Description = "Test Description",
                Start = DateTime.Now.AddDays(2),
                End = DateTime.Now.AddHours(1)
            };
            // Define a user ID for testing purposes
            string userIdTwo = "testUserIdTwo";

            // Add the two events to the database using the event service
            await _eventService.AddEventAsync(eventModelOne, userIdOne);
            await _eventService.AddEventAsync(eventModelTwo, userIdTwo);

            // Step 2: Act - Perform the action being tested
            
            var result = await _eventService.GetAllEventsAsync();
            
            // Step 3: Act - Retrieve the count of events from the database

            // Step 4: Assert - Verify the outcome of the action
            // Assert that the count of events in the database is equal to the expected count (2)
            Assert.That(result.Count(), Is.EqualTo(2));
 
        }

        [Test]
        public async Task GetEventDetailsAsync_ShouldReturnAllEventDetails()
        {
            //arrange
            var eventModel = new EventFormModel
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = 2
            };

            await _eventService.AddEventAsync(eventModel, "nonExistingUserId");

            var eventInTheDb = await _dbContext.Events.FirstAsync();

            //act
            var result = await _eventService.GetEventDetailsAsync(eventInTheDb.Id);

            //assert
            Assert.IsNotNull(result);
            Assert.That(result.Name, Is.EqualTo(eventModel.Name));
            Assert.That(result.Description, Is.EqualTo(eventModel.Description));
        }

        [Test]
        public async Task GetEventForEditAsync_ShouldGetEventIfPresentInDb()
        {
            //arrange
            var eventModel = new EventFormModel
            {
                Name = "Test Event",
                Description = "Demo Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = 2
            };

            await _eventService.AddEventAsync(eventModel, "nonExistingUser");
            var eventIntheDb = await _dbContext.Events.FirstAsync();

            //act
            var result = await _eventService.GetEventForEditAsync(eventIntheDb.Id);

            //assert
            Assert.IsNotNull(result);
            Assert.That(result.Name, Is.EqualTo(eventModel.Name));
            Assert.That(result.Description, Is.EqualTo(eventModel.Description));
            Assert.That(result.End, Is.EqualTo(eventModel.End));
            Assert.That(result.Start, Is.EqualTo(eventModel.Start));
            Assert.That(result.TypeId, Is.EqualTo(eventModel.TypeId));
        }

        [Test]
        public async Task GetEventForEditAsyn_ShouldReturnNullIfEventIsNotInDB()
        {
            //act
            var result = await _eventService.GetEventForEditAsync(90);
            //assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetEventOrganizerIdAsync_ShouldReturnOrganizerIdIfExisting()
        {
            //arrange
            var eventModel = new EventFormModel
            {
                Name = "Test Event",
                Description = "Demo Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = 2
            };

            const string userId = "userID";
            await _eventService.AddEventAsync(eventModel, userId);
            var eventIntheDb = await _dbContext.Events.FirstAsync();

            //act
            var result = await _eventService.GetEventOrganizerIdAsync(eventIntheDb.Id);

            //assert
            Assert.IsNotNull(result);
            Assert.That(result, Is.EqualTo(userId)); 
        }

        [Test]
        public async Task GetEventOrganizerIdAsync_ShouldReturnNullIfNotExisting()
        {
            var result = await _eventService.GetEventOrganizerIdAsync(99);
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetUserJoinedEventsAsync_ShouldReturnAllJoinedUsers()
        {
            //arrange
            const string userId = "userId";

            var testType = new Data.Models.Type
            {
                Name = "Test Name"
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            var testEvent = new Event
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = userId
            };

            await _dbContext.SaveChangesAsync();
            await _dbContext.Events.AddAsync(testEvent);
            

            await _dbContext.EventsParticipants.AddAsync(new EventParticipant()
            {
                EventId = testEvent.Id,
                HelperId = userId
            });
            await _dbContext.SaveChangesAsync();

            //act
            var result = await _eventService.GetUserJoinedEventsAsync(userId);

            //assert
            Assert.IsNotNull(result);
            Assert.That(result.Count(), Is.EqualTo(1));

            var eventParticipation = result.First();

            Assert.That(eventParticipation.Id, Is.EqualTo(testEvent.Id));
            Assert.That(eventParticipation.Name, Is.EqualTo(testEvent.Name));
            Assert.That(eventParticipation.Type, Is.EqualTo(testType.Name));
            
        }

        [Test]
        public async Task JoinEventAsync_ShouldReturnFalseIfEventDoesNotExist()
        {
            //act
            var result = await _eventService.JoinEventAsync(99, "");
            //assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task JoinEventAsync_ShouldReturnFalseIfUserIsAlreadyPartOfEvent()
        {
            // arrange
            const string userId = "userId";

            // add an event type to the DB
            var testType = new Data.Models.Type
            {
                Name = "Test Name"
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            // add an event to the DB
            var testEvent = new Event
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = userId
            };

            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();
            

            // add user to the event
            await _dbContext.EventsParticipants.AddAsync(new EventParticipant()
            {
                EventId = testEvent.Id,
                HelperId = userId
            });
            await _dbContext.SaveChangesAsync();

            // act
            var result = await _eventService.JoinEventAsync(testEvent.Id, userId);
            //arrange
            Assert.IsFalse(result);
        }

        [Test]
        public async Task JoinEventAsync_ShouldReturnTrueIfUserIsAddedToTheEvent()
        {
            // arrange
            const string userId = "userId";

            // add an event type to the DB
            var testType = new Data.Models.Type
            {
                Name = "Test Name"
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            // add an event to the DB
            var testEvent = new Event
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = userId
            };

            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();

            //act
            var result = await _eventService.JoinEventAsync(testEvent.Id, userId);

            //assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task LeaveEventAsync_ShouldReturnFalseIfLeaveWithoutBeingPartOfEvent()
        {
            //arrange
            const string userId = "UserID";
            //act
            var result = await _eventService.LeaveEventAsync(99, userId);
            //assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task LeaveEventAsync_ShouldReturnTrueIfWeLeaveEvent()
        {
            //arrange
            var testType = new Data.Models.Type
            {
                Name = "Test Name"
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            var testEvent = new Event
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = "placeholder-user"
            };

            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();

            string userId = "new-guest";
            await _eventService.JoinEventAsync(testEvent.Id, userId);

            //act
            var result = await _eventService.LeaveEventAsync(testEvent.Id, userId);
            //assert
            Assert.IsTrue(result);

        }

        [Test]
        public async Task UpdateEventAsync_ShouldReturnFalseIfEventDoesNotExist()
        {
            //arrange

            //act
            var result = await _eventService.UpdateEventAsync(999, new EventFormModel { }, "user-id");
            //assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task UpdateEventAsync_ShouldReturnFalseIfTheOrganiserIsDifferent()
        {
            const string firstUserId = "first-user-id";
            const string secondUserId = "second-user-id";
            //arrange
            // add an event type to the DB
            var testType = new Data.Models.Type
            {
                Name = "Test Name"
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            // add an event to the DB
            var testEvent = new Event
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = firstUserId
            };

            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();

            //act
            var result = await _eventService.UpdateEventAsync(testEvent.Id, new EventFormModel { }, secondUserId);

            //assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task UpdateEventAsync_ShouldReturnTrueIfTheOrganiserIsCorrect()
        {
            const string firstUserId = "first-user-id";
            
            //arrange
            // add an event type to the DB
            var testType = new Data.Models.Type
            {
                Name = "Test Name"
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            // add an event to the DB
            var testEvent = new Event
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = firstUserId
            };

            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();

            //act
            var result = await _eventService.UpdateEventAsync(
                testEvent.Id,
                new EventFormModel
                {
                    Name = "Updated Name Event",
                    Description = testEvent.Description,
                    Start = testEvent.Start,
                    End = testEvent.End,
                    TypeId = testType.Id,
                },
                firstUserId);

            //assert
            Assert.IsTrue(result);

            var eventFromTheDb = await _dbContext.Events.FirstOrDefaultAsync(x => x.Id == testEvent.Id);
            Assert.IsNotNull(eventFromTheDb);
            Assert.That(eventFromTheDb.Name, Is.EqualTo("Updated Name Event"));
        }

        [Test]
        public async Task GetAllTypesAsync_ShouldReturnAllTypes()
        {
            //arrange
            var testType = new Data.Models.Type
            {
                Name = "test-type"
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            //act
            var result = await _eventService.GetAllTypesAsync();
            //assert
            Assert.That(result.Count, Is.EqualTo(1));

            var singleType = result.First();
            Assert.That(singleType.Name, Is.EqualTo(testType.Name));
        }

        [Test]
        public async Task IsUserJoinedEventAsync_ShouldReturnFalseIfEventDoesNotExist()
        {
            //act
            var result = await _eventService.IsUserJoinedEventAsync(1, "test-user");
            //assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task IsUserJoinedEventAsync_ShouldReturnFalseIfUserDoesNotExist()
        {
            //arrange
            // add an event type to the DB
            var testType = new Data.Models.Type
            {
                Name = "Test Name"
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            // add an event to the DB
            var testEvent = new Event
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = "placeholder-user"
            };

            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();

            //act
            var result = await _eventService.IsUserJoinedEventAsync(testEvent.Id, "non-existing-user");
            //assert
            Assert.That(result, Is.False);

        }

        [Test]
        public async Task IsUserJoinedEventAsync_ShouldReturnTrueIfUserIsInEvent()
        {
            //arrange
            // add an event type to the DB
            var testType = new Data.Models.Type
            {
                Name = "Test Name"
            };
            await _dbContext.Types.AddAsync(testType);
            await _dbContext.SaveChangesAsync();

            // add an event to the DB
            var testEvent = new Event
            {
                Name = "Test Event",
                Description = "Test Description",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                TypeId = testType.Id,
                OrganiserId = "placeholder-user"
            };

            await _dbContext.Events.AddAsync(testEvent);
            await _dbContext.SaveChangesAsync();

            await _eventService.JoinEventAsync(testEvent.Id, "joined-id");

            //act
            var result = await _eventService.IsUserJoinedEventAsync(testEvent.Id, "joined-id");
            //assert
            Assert.That(result, Is.True); 
        }
    }
}
