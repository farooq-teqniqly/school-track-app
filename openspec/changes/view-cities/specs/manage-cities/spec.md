## ADDED Requirements

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
