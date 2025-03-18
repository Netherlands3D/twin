# Page Object Model (POM) for testing

The Page Object Model (POM) is a design pattern that provides a structured way to interact with and retrieve information
from the user interface and world view while minimizing duplication across tests. 

Instead of directly interacting with UI elements in every test case, POM defines component objects that encapsulate how 
different parts of the UI can be controlled. In a typical web application, this pattern is applied at the page level, 
but in Netherlands3D — where the UI functions more like a desktop application with 3D map interactions — each UI 
component (such as the toolbar, layer panel, or map view) is treated as a distinct object. These objects expose clear 
methods to perform actions, like `FindLayer()`, `HideLayer()`, or `Zoom()`, making it easier to write tests that focus 
on what needs to be tested rather than how the UI works internally.

By following POM, we achieve better maintainability, reusability, and separation of concerns in automated testing. 
Because UI interactions are centralized in dedicated component objects, changes to the UI (such as renaming a button or 
modifying a workflow) only require updates in one place, rather than across multiple testcases.

## Read more

- https://martinfowler.com/bliki/PageObject.html
- https://www.selenium.dev/documentation/test_practices/encouraged/page_object_models/
- https://webdriver.io/docs/pageobjects/
- https://testcafe.io/documentation/402826/guides/best-practices/page-model