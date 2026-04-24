Feature: Authenticated task lifecycle
  In order to track my work end-to-end
  As a registered user of the Task Management API
  I want to create, list and complete tasks through authenticated HTTP calls

  Background:
    Given I am a registered and authenticated user

  Scenario: User creates, lists and completes a task
    When I create a task titled "Draft the release notes"
    Then the task list contains a task titled "Draft the release notes" with status "Pending"
    When I mark the task titled "Draft the release notes" as complete
    Then the task list contains a task titled "Draft the release notes" with status "Completed"

  Scenario: Unauthenticated caller cannot list tasks
    Given I discard my authentication
    When I request the task list
    Then the response status code is 401
