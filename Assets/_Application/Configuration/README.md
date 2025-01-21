Twin Configuration
==================

This system is responsible for reading the application configuration from a configuration file, the URI or both. In its
essence, you define which features should be enabled and each of these features can have their own configuration and
events.

This system contains a variety of Scriptable Objects so that individual parts of the application can listen in on events
in these. 

The `ConfiguratorStarter` class is the main entrypoint for the configuration system and is responsible for waiting
until all other parts of the application have loaded before executing the `Configurator`. The configurator is 
responsible for properly loading the `Configuration` object and by emitting events to signal when this has commenced and 
completed.

> As an example of use: the progress bar listens on these events to show a progressbar and disable input while doing so.

It is the `Configurator` that features the properties needed to influence how the configuration is loaded and if there
is not enough information: to load the `SetupWizard` scene to complete the configuration. The Configurator also features
a `debugUrl` field with which to simulate the URL when you are working from the editor.

The `Configuration` Scriptable Object contains the top-most application configuration and can be (de)serialized to and 
from a JSON configuration file; it also contains the logic to populate its fields from a given URL.

# Constructing a JSON Config file

The easiest path is to configure the application in the desired state (which can also be done during Runtime), and then 
use the context menu of the `Configurator` to "Write the Config to JSON file". This will serialize the configuration
to a JSON file, which you can deploy.