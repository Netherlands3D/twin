UI Kit
======

To provide a consistent UI theme in the application this package contains a UI Kit with default components and styling.
As this is based on [best practices](https://docs.unity3d.com/Manual/UIE-USS-WritingStyleSheets.html described in the 
Unity documentation) and by making use of [USS variables](https://docs.unity3d.com/Manual/UIE-USS-variables.html), the
styling can be extended or adjusted in client projects.

Structure
---------

This package contains a `common.blocks` folder -per the [BEM recommendation](https://en.bem.info/methodology/redefinition-levels/)-
that contains a folder per block. Within this folder, a USS file can be found for that block. For blocks containing
a significant number of elements, additional USS files may be created to match the name of that element. This matches
the [Flex folder structure](https://en.bem.info/methodology/filestructure/#flex) of BEM.

Naming Conventions
------------------

The naming convention for UXML elements and USS classes follow the [BEM Naming Conventions](https://en.bem.info/methodology/naming-convention/). 
USS variables follow the [convention Unity uses for its built-in variables](https://docs.unity3d.com/Manual/UIE-USS-UnityVariables.html), 
where we use `nl3d` as prefix:

```
--nl3d-{group}-{role_and_control}-{sub_element}-{pseudo_state_sequence}
```

Applying the BEM naming conventions also means that a selector within a subfolder of the `common.blocks` folder always
starts with the name of that block. Thus if there is a folder `common.blocks/inspector` then you know this is all about
the `inspector` block, and each selector in the USS files starts at least with `inspector`.