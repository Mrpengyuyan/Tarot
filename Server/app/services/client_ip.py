from __future__ import annotations

import ipaddress
from typing import Iterable, Optional, Tuple, Union

from fastapi import Request

from app.core.config import settings

IPAddress = Union[ipaddress.IPv4Address, ipaddress.IPv6Address]
IPNetwork = Union[ipaddress.IPv4Network, ipaddress.IPv6Network]


def parse_trusted_proxy_networks(raw: str) -> Tuple[IPNetwork, ...]:
    networks = []
    for item in (raw or "").split(","):
        entry = item.strip()
        if not entry:
            continue
        networks.append(ipaddress.ip_network(entry, strict=False))
    return tuple(networks)


def _parse_ip(raw: str) -> Optional[IPAddress]:
    candidate = (raw or "").strip()
    if not candidate:
        return None
    if candidate.startswith("["):
        end_bracket = candidate.find("]")
        if end_bracket == -1:
            return None
        candidate = candidate[1:end_bracket]
    elif candidate.count(":") == 1:
        # Strip optional IPv4 :port suffix while leaving IPv6 literals intact.
        host_part, _, port_part = candidate.partition(":")
        if port_part.isdigit():
            candidate = host_part
    try:
        return ipaddress.ip_address(candidate)
    except ValueError:
        return None


def _is_in_networks(address: IPAddress, networks: Iterable[IPNetwork]) -> bool:
    return any(address in network for network in networks)


def resolve_client_host(
    request: Request,
    *,
    trusted_proxy_ips: Optional[str] = None,
) -> str:
    """Resolve the client IP for rate limiting without allowing X-Forwarded-For spoofing.

    Only inspects X-Forwarded-For when the immediate TCP peer belongs to a configured
    trusted proxy IP/CIDR. Walks the header right-to-left and picks the right-most
    hop that is not itself a trusted proxy.
    """
    direct_host = (request.client.host or "").strip() if request.client else ""
    if not direct_host:
        return "unknown"

    networks = (
        parse_trusted_proxy_networks(trusted_proxy_ips)
        if trusted_proxy_ips is not None
        else settings.trusted_proxy_networks
    )
    if not networks:
        return direct_host

    direct_ip = _parse_ip(direct_host)
    if direct_ip is None or not _is_in_networks(direct_ip, networks):
        return direct_host

    forwarded_for = request.headers.get("x-forwarded-for", "")
    if not forwarded_for.strip():
        return str(direct_ip)

    hops = [part.strip() for part in forwarded_for.split(",") if part.strip()]
    if not hops:
        return str(direct_ip)

    last_valid_ip: Optional[IPAddress] = None
    for hop in reversed(hops):
        hop_ip = _parse_ip(hop)
        if hop_ip is None:
            return str(direct_ip)
        last_valid_ip = hop_ip
        if not _is_in_networks(hop_ip, networks):
            return str(hop_ip)

    return str(last_valid_ip) if last_valid_ip is not None else str(direct_ip)
