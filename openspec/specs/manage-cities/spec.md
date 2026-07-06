## ADDED Requirements

### Requirement: State is a closed set of U.S. states and D.C.
The system SHALL provide a `State` value object representing a fixed set of the 50 U.S. states plus the District of Columbia, each identified by its two-letter abbreviation.

#### Scenario: States are equal by abbreviation
- **WHEN** two `State` instances with the same two-letter abbreviation (regardless of case) are compared
- **THEN** the system SHALL consider them equal

#### Scenario: States with different abbreviations are not equal
- **WHEN** two `State` instances with different two-letter abbreviations are compared
- **THEN** the system SHALL consider them not equal

### Requirement: Create a city with a name and state
The system SHALL create a `City` with a required `Name` (non-empty, maximum 100 characters) and a required `State`.

#### Scenario: Create a valid city
- **WHEN** a city is created with a non-empty name of 100 characters or fewer and a valid state
- **THEN** the system SHALL create a `City` with a new `CityId`, the supplied name, and the supplied state

#### Scenario: City name must not be empty
- **WHEN** a city is created with an empty or whitespace-only name
- **THEN** the system SHALL reject the operation and create no city

#### Scenario: City name must not exceed maximum length
- **WHEN** a city is created with a name longer than 100 characters
- **THEN** the system SHALL reject the operation and create no city

#### Scenario: City state is required
- **WHEN** a city is created with a null state
- **THEN** the system SHALL reject the operation and create no city

### Requirement: Cities are equal by name and state
Two `City` instances SHALL be considered equal when their names match case-insensitively and their states are equal.

#### Scenario: Cities with same name and state are equal
- **WHEN** two `City` instances have the same name (regardless of case) and the same state
- **THEN** the system SHALL consider them equal

#### Scenario: Cities with different states are not equal
- **WHEN** two `City` instances have the same name but different states
- **THEN** the system SHALL consider them not equal

#### Scenario: Cities with different names are not equal
- **WHEN** two `City` instances have different names but the same state
- **THEN** the system SHALL consider them not equal

### Requirement: Admin users can add cities in a batch
An authenticated user in the Admin role SHALL be able to submit a batch of 1 to 100 cities for persistence in a single operation.

#### Scenario: Successful batch save
- **WHEN** an Admin submits a batch of 1 to 100 valid, non-duplicate cities
- **THEN** the system SHALL persist all cities in the batch
- **AND** SHALL display a success notification

#### Scenario: Batch save rejects on any invalid row
- **WHEN** an Admin submits a batch containing at least one city with an invalid name or missing state
- **THEN** the system SHALL persist no cities from the batch
- **AND** SHALL display the validation error to the user

#### Scenario: Batch save rejects on duplicate within the batch
- **WHEN** an Admin submits a batch containing two or more cities that are equal to each other (same name and state)
- **THEN** the system SHALL persist no cities from the batch
- **AND** SHALL display an error indicating the duplicate

#### Scenario: Batch save rejects on duplicate against existing data
- **WHEN** an Admin submits a batch containing a city that is equal to (same name and state as) a city already persisted
- **THEN** the system SHALL persist no cities from the batch
- **AND** SHALL display a save-failed notification

#### Scenario: Non-Admin user cannot access the Add Cities form
- **WHEN** an authenticated user who is not in the Admin role attempts to access the Add Cities form
- **THEN** the system SHALL deny access

### Requirement: City persistence records creation metadata
When a `City` is persisted, the system SHALL record the UTC creation timestamp, the identifier of the user who created it, and assign a unique identifier. Creation metadata is stamped by `AuditInterceptor` — services SHALL NOT set `CreatedByUserId` or `CreatedAt` manually.

#### Scenario: Created date is set on persistence
- **WHEN** a city is persisted
- **THEN** the system SHALL set its created date to the current UTC time

#### Scenario: Creating user is recorded on persistence
- **WHEN** an Admin submits a batch that is persisted
- **THEN** the system SHALL record that Admin's `RegisteredUserId` as the creator of each city in the batch

### Requirement: Admin users can view a list of persisted cities
An authenticated user in the Admin role SHALL be able to view a list of the
cities that have been persisted, showing each city's name, state, and the
identity of the user who created it.

#### Scenario: List displays persisted cities
- **WHEN** an Admin opens the View Cities page and one or more cities exist
- **THEN** the system SHALL display each persisted city's name, state, and creator

#### Scenario: Creator is shown as the creating user's email
- **WHEN** a displayed city was created by a user whose account resolves to an email address
- **THEN** the system SHALL display that email as the city's creator

#### Scenario: Creator falls back to the raw identifier when unresolved
- **WHEN** a displayed city's creating user cannot be resolved to an email address
- **THEN** the system SHALL display the raw creating-user identifier as the creator

#### Scenario: Empty catalog shows an empty state
- **WHEN** an Admin opens the View Cities page and no cities exist
- **THEN** the system SHALL display a message indicating that no cities exist
- **AND** SHALL display no city rows

#### Scenario: Cities are ordered by state then name
- **WHEN** the list of cities is displayed
- **THEN** the system SHALL order the cities by state abbreviation ascending, then by city name ascending, both case-insensitively

#### Scenario: Non-Admin user cannot access the View Cities page
- **WHEN** an authenticated user who is not in the Admin role attempts to access the View Cities page
- **THEN** the system SHALL deny access

### Requirement: City list can be searched by name
The system SHALL filter the displayed cities to those whose name contains a
supplied search term, case-insensitively.

#### Scenario: Search narrows the list by name
- **WHEN** an Admin enters a search term
- **THEN** the system SHALL display only cities whose name contains that term, ignoring case
- **AND** SHALL exclude cities whose name does not contain the term

#### Scenario: Search with no matches shows an empty result
- **WHEN** an Admin enters a search term that matches no city name
- **THEN** the system SHALL display no city rows
- **AND** SHALL display a message indicating that no cities match

### Requirement: City list can be filtered by state
The system SHALL filter the displayed cities to those located in a supplied
state.

#### Scenario: State filter narrows the list
- **WHEN** an Admin selects a state
- **THEN** the system SHALL display only cities located in that state
- **AND** SHALL exclude cities located in other states

#### Scenario: Clearing the state filter shows all states
- **WHEN** an Admin clears the state selection
- **THEN** the system SHALL display cities regardless of state

### Requirement: City list is paged
The system SHALL return the cities one page at a time, given a page number and
a fixed page size, together with the total count of cities matching the current
search and state filter.

#### Scenario: A page returns at most the page size
- **WHEN** more cities match than the page size
- **THEN** the system SHALL return no more than the page size number of cities for the requested page
- **AND** SHALL report the total count of matching cities

#### Scenario: Changing the search or state filter resets to the first page
- **WHEN** an Admin changes the search term or the state filter
- **THEN** the system SHALL display the first page of the newly filtered results
