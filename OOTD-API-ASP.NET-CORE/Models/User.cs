using System;
using System.Collections.Generic;

namespace OOTD_API.Models;

public partial class User
{
    public int Uid { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Address { get; set; } = null!;

    public bool IsAdministrator { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool Enabled { get; set; }

    public virtual ICollection<CartProduct> CartProducts { get; set; } = new List<CartProduct>();

    public virtual ICollection<Message> MessageReceivers { get; set; } = new List<Message>();

    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();

    public virtual ICollection<UserCoupon> UserCoupons { get; set; } = new List<UserCoupon>();
}
