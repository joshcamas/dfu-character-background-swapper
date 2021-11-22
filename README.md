# dfu-character-background-swapper

Adds support for paperdoll background swapping depending on variables

### Night
If it is night, a "_NIGHT" texture will be used, if it exists.

Example: ``SCBG00I0_NIGHT.IMG``

### Weather
The weather tags are as follows: "_RAIN", "_STORM", "_FOG", "_OVERCAST", "_WINTER".

Example: ``SCBG00I0_RAIN.IMG``

### Interior
Interior will make the base image name be interior.IMG. So, to make an image appear in all interiors (dungeons, shops) you could just make a interior.IMG.png file!

However, there are several other tags: "_TEMPLE", "_CASTLE", "_DUNGEON", "_OPENSHOP", "_TAVERN".

### Combinations
Weather and Time can be combo'd however you'd like!

Example: ``SCBG00I0_NIGHT_SNOW.IMG``
Example: ``SCBG00I0_SNOW_NIGHT.IMG``

I'm guessing you don't want to make rain+storm variants, so you can just have a copied image for both storm and rain. Same for overcast, totally up to you!

### Known Issues

Incompactible with HotkeyBar
