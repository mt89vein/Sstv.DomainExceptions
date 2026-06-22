using Sstv.Domain.Sample;
using Sstv.DomainExceptions;

[assembly: CollectErrorCodes(Types =
[
    typeof(ErrorCodes),
    typeof(SecondErrorCodes),
    typeof(DomainErrorCodes)
])]
