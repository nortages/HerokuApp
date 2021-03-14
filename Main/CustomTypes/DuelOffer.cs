using System;

namespace HerokuApp.Main
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    internal class DuelOffer
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        public string WhoOffers { get; }
        public string WhomIsOffered { get; }

        public DuelOffer(string whoOffers, string whoIsOffered)
        {
            WhoOffers = whoOffers;
            WhomIsOffered = whoIsOffered;
        }

        public override bool Equals(object obj)
        {
            return (obj is DuelOffer offer) && offer.WhoOffers == WhoOffers && offer.WhomIsOffered == WhomIsOffered;
        }
    }
}