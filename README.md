protobuf-linq
=============

A LINQ extension to the great [protobuf-net](http://code.google.com/p/protobuf-net/) library provided by Marc Gravell.
The library provides an interesting opportunity to speed up queryies over big stream of data, as it offers an optimal deserialization of items. Typical scenarios to use it would be:
- counting elements in the collection
- counting elements of the given properties
- selecting a subset of properties as a projection from the type
- using _OfType_ to select a subset of the messages of a given type
- using _Where_ to filter out not needed information

The _protobuf-linq_ does not implement IQueryable, rather providing custom methods which makes it look like IQueryable. The LINQ syntax, for these who prefer it is appliable as well.

*The speed bust*
In a few performance tests included in the test project, it is shown that querying with protobuf-linq over a big set of data can greatly improve an execution time reducing it to the ~50% of the original. The size of allocations is also reduced.
The reduction of the cost depends on the type of operation and may not be so significant in some scenarios.
